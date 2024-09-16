// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;

namespace HyperVExtension.Models;

/// <summary> A class that represents the result of a PowerShell command </summary>
public class PowerShellResult : PowerShellResultBase
{
    /// <inheritdoc cref="PowerShellResultBase.PsObject"/>
    public override Collection<PSObject> PsObjects { get; set; } = new();

    /// <inheritdoc cref="PowerShellResultBase.CommandOutputErrorMessage"/>
    public override string CommandOutputErrorMessage { get; set; } = string.Empty;

    /// <inheritdoc cref="PowerShellResultBase.CommandOutputErrorFirstHresult"/>
    public override int CommandOutputErrorFirstHResult { get; set; }
}
