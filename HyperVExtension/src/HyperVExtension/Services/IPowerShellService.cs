// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;

namespace HyperVExtension.Services;

/// <summary> Enum that represents the type of piping to use when executing PowerShell statements.</summary>
public enum PipeType
{
    None,
    PipeOutput,
    DontClearBetweenStatements,
}

/// <summary> Interface that handles PowerShell command line statements. </summary>
public interface IPowerShellService
{
    /// <summary> Executes a single PowerShell commandline statement. </summary>
    /// <param name="commandLineStatement"> A list that houses commandline statements that can be run by PowerShell.</param>
    /// <returns> An object that provides error information as well as the list of PSOjects returned by PowerShell.</returns>
    public PowerShellResult Execute(PowerShellCommandlineStatement commandLineStatement);

    /// <summary> Executes multiple PowerShell commandline statements. </summary>
    /// <param name="commandLineStatements"> A list that houses commandline statements that can be run by PowerShell.</param>
    /// <param name="pipeType"> Type of piping to use when running the commandline statements.</param>
    /// <returns> An object that provides error information as well as the list of PSOjects returned by PowerShell.</returns>
    public PowerShellResult Execute(IEnumerable<PowerShellCommandlineStatement> commandLineStatements, PipeType pipeType);
}
