// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;

namespace HyperVExtension.Models;

/// <summary> The base class for all PowerShell results </summary>
public abstract class PowerShellResultBase
{
    /// <summary> Gets or sets the base object of the PowerShell result. </summary>
    /// <remarks>
    /// This is the PowerShell object that is returned from the invoke method
    /// inside the PowerShell session.
    /// </remarks>
    public abstract Collection<PSObject> PsObjects { get; set; }

    /// <summary> Gets or sets the error message return by PowerShell. </summary>
    /// <remark>
    /// This is the error message that is returned when the command returns an error
    /// in the PowerShell runspace. This is used for logging and debugging purposes.
    /// </remark>
    public abstract string CommandOutputErrorMessage { get; set; }
}
