// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using HyperVExtension.Models;

namespace HyperVExtension.UnitTest.Mocks;

public class PowerShellSessionMock : IPowerShellSession
{
    public Collection<PSObject> TestResultCollection { get; set; } = new();

    public string ErrorText { get; set; } = string.Empty;

    /// <inheritdoc cref="IPowerShellSession.AddCommand(string)"/>
    public void AddCommand(string command)
    {
    }

    /// <inheritdoc cref="IPowerShellSession.AddParameters(IDictionary)"/>
    public void AddParameters(IDictionary parameters)
    {
    }

    /// <inheritdoc cref="IPowerShellSession.AddScript(string, bool)"/>
    public void AddScript(string script, bool useLocalScope)
    {
    }

    /// <inheritdoc cref="IPowerShellSession.Invoke"/>
    public Collection<PSObject> Invoke()
    {
        return TestResultCollection;
    }

    /// <inheritdoc cref="IPowerShellSession.ClearSession"/>
    public void ClearSession()
    {
    }

    /// <inheritdoc cref="IPowerShellSession.GetErrorMessages"/>
    public string GetErrorMessages()
    {
        return ErrorText;
    }

    /// <inheritdoc cref="IPowerShellSession.GetErrorFirstHResult"/>
    public int GetErrorFirstHResult()
    {
        return 0;
    }
}
