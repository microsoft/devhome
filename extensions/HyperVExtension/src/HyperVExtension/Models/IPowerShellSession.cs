// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper interface for interacting with the PowerShell class in the System.Management.Automation
/// assembly.
/// </summary>
public interface IPowerShellSession
{
    /// <summary> Adds a command to the PowerShell session. </summary>
    /// <param name="command">A string representing the name of a cmdlet that can be run by PowerShell</param>
    public void AddCommand(string command);

    /// <summary> Adds parameters to the PowerShell session. </summary>
    /// <param name="parameters">
    /// Key value pairs where the key is the parameter name and the value is the value
    /// associated with the parameter.
    /// </param>
    public void AddParameters(IDictionary parameters);

    /// <summary> Adds a script to the PowerShell session. </summary>
    /// <param name="script">A string representing a script that can be run by PowerShell.</param>
    public void AddScript(string script, bool useLocalScope);

    /// <summary> Invokes the PowerShell statements. </summary>
    /// <returns>A collection of PowerShell Objects returned by PowerShell.</returns>
    public Collection<PSObject> Invoke();

    /// <summary> Clears the PowerShell session by removing all commands and error streams. </summary>
    public void ClearSession();

    /// <summary> Gets the error messages associated with this instance of the PowerShell session. </summary>
    public string GetErrorMessages();

    /// <summary> Gets the first error HRESULT associated with this instance of the PowerShell session. </summary>
    public int GetErrorFirstHResult();
}
