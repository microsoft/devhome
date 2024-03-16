// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper class for interacting with the PowerShell class in the System.Management.Automation
/// assembly.
/// </summary>
public class PowerShellSession : IPowerShellSession, IDisposable
{
    private readonly PowerShell _powerShellSession;
    private bool _disposedValue;

    public PowerShellSession()
    {
        _powerShellSession = PowerShell.Create();
    }

    /// <inheritdoc cref="IPowerShellSession.AddCommand(string)"/>
    public void AddCommand(string command)
    {
        _powerShellSession.AddCommand(command);
    }

    /// <inheritdoc cref="IPowerShellSession.AddParameters(IDictionary)"/>
    public void AddParameters(IDictionary parameters)
    {
        _powerShellSession.AddParameters(parameters);
    }

    /// <inheritdoc cref="IPowerShellSession.AddScript(string)"/>
    public void AddScript(string script, bool useLocalScope)
    {
        _powerShellSession.AddScript(script, useLocalScope);
    }

    /// <inheritdoc cref="IPowerShellSession.Invoke"/>
    public Collection<PSObject> Invoke()
    {
        return _powerShellSession.Invoke();
    }

    /// <inheritdoc cref="IPowerShellSession.ClearSession"/>
    public void ClearSession()
    {
        _powerShellSession.Commands.Clear();
        _powerShellSession.Streams.ClearStreams();
    }

    /// <inheritdoc cref="IPowerShellSession.GetErrorMessages"/>
    public string GetErrorMessages()
    {
        return string.Join(Environment.NewLine, _powerShellSession.Streams.Error.Select(err => err.Exception.Message));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _powerShellSession.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
