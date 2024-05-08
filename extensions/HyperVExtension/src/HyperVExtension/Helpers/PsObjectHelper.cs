// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Serilog;

namespace HyperVExtension.Helpers;

/// <summary>
/// Helper class for interacting with <see cref="System.Management.Automation.PSObject"/>s
/// at runtime. This is used for cases where we don't know the the underlying object types
/// inside the <see cref="System.Management.Automation.PSObject"/> at compile time.
/// </summary>
public class PsObjectHelper
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(PsObjectHelper));

    private readonly PSObject _psObject;

    public PsObjectHelper(in PSObject pSObject)
    {
        _psObject = pSObject;
    }

    /// <summary>
    /// Method to extract an object from the member collection located in the psObject.
    /// This is used for cases where we don't know the PSObject's base object type at compile time.
    /// PSObject documentation here: https://learn.microsoft.com/dotnet/api/system.management.automation.psobject?view=powershellsdk-7.3.0
    /// </summary>
    /// <remarks>
    /// PSObjects contain a member array that holds properties and methods for the base object it wraps. Each item in the array
    /// is a key value pair, where the key is the name of the property or method and the value is the value of the property
    /// or output of the method call. When we invoke a PowerShell command using the System.Management.Automation assembly's
    /// PowerShell class the returned result is a list of PsObject's. Each PsObject wraps an underlying base object of type
    /// 'Object'. If we know the underlying base object type at compile time e.g if we know PowerShell will return a string as the
    /// base object, we can cast it to the string type directly without needing to use this method. However, The Hyper-V
    /// PowerShell module is loaded into the PowerShell runspace/session at run time and we do not have access to the custom
    /// types it holds until then. So this prevents us from statically knowing the types, hence why we have this generic
    /// method to make getting information from these custom types simpler. Note: These custom types and their values can be
    /// found by opening a PowerShell window and piping the object to the Get-Member PowerShell cmdlet.
    /// E.g Get-VM -Name <VMName> | Get-Member.
    /// </remarks>
    public T? MemberNameToValue<T>(string memberName)
    {
        try
        {
            var potentialValue = _psObject.Members?[memberName]?.Value;

            if (potentialValue is T memberValue)
            {
                return memberValue;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get member value with name {memberName}");
        }

        return default(T);
    }

    /// <summary>
    /// Method to extract the value of an object's property using reflection.
    /// This is used for cases where we don't know the inputted objects type at compile time.
    /// </summary>
    public T? PropertyNameToValue<T>(in object obj, string propertyName)
    {
        var type = obj.GetType();

        try
        {
            var property = type.GetProperty(propertyName);
            var potentialValue = property?.GetValue(obj, null);

            if (potentialValue is T propertyValue)
            {
                return propertyValue;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get property value with name {propertyName} from object with type {type}.");
        }

        return default(T);
    }
}
