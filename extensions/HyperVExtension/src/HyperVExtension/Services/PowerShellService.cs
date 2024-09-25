// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;
using HyperVExtension.Common;
using HyperVExtension.Models;
using Serilog;

namespace HyperVExtension.Services;

/// <summary> Class that handles PowerShell commands.</summary>
public class PowerShellService : IPowerShellService, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(PowerShellService));

    private readonly IStringResource _stringResource;

    private readonly IPowerShellSession _powerShellSession;

    private readonly object _powerShellSessionLock = new();

    private bool _disposed;

    public PowerShellService(IStringResource stringResource, IPowerShellSession powerShellSession)
    {
        _stringResource = stringResource;
        _powerShellSession = powerShellSession;
    }

    /// <inheritdoc cref="IPowerShellService.Execute(PowerShellCommandlineStatement)"/>
    public PowerShellResult Execute(PowerShellCommandlineStatement commandLineStatement)
    {
        return Execute(new List<PowerShellCommandlineStatement> { commandLineStatement }, PipeType.None);
    }

    /// <inheritdoc cref="IPowerShellService.Execute(IEnumerable{PowerShellCommandlineStatement}, PipeType)"/>
    /// <remarks>
    /// When PipeType is set to PipeOutput, the order of the statements in the list are important.
    /// The output of the first statement will be piped to the second statement and so on.
    /// </remarks>
    public PowerShellResult Execute(IEnumerable<PowerShellCommandlineStatement> commandLineStatements, PipeType pipeType)
    {
        try
        {
            lock (_powerShellSessionLock)
            {
                // Clear the PowerShell commands and streams to ensure that the previous commands are not run again.
                _powerShellSession.ClearSession();
                var psObjectList = ExecuteStatements(commandLineStatements, pipeType);
                var commandOutputErrorMessage = _powerShellSession.GetErrorMessages();
                var hresult = _powerShellSession.GetErrorFirstHResult();

                return new PowerShellResult
                {
                    PsObjects = psObjectList,
                    CommandOutputErrorMessage = commandOutputErrorMessage,
                    CommandOutputErrorFirstHResult = hresult,
                };
            }
        }
        catch (Exception ex)
        {
            var commandStrings = string.Join(Environment.NewLine, commandLineStatements.Select(cmd => cmd.ToString()));
            _log.Error(ex, $"Error running PowerShell commands: {commandStrings}");
            throw;
        }
    }

    /// <summary> Runs the PowerShell statements based on the pipe type passed in.</summary>
    private Collection<PSObject> ExecuteStatements(IEnumerable<PowerShellCommandlineStatement> commandLineStatements, PipeType pipeType)
    {
        if (pipeType == PipeType.PipeOutput)
        {
            return BuildPipelineAndExecuteStatements(commandLineStatements);
        }

        return ExecuteIndividualStatements(commandLineStatements, pipeType);
    }

    /// <summary>
    /// Builds the PowerShell pipeline with the command line statements. The output from the previous
    /// statement is piped as input for the next statement.
    /// </summary>
    /// <param name="commandLineStatements"> A list of statements that houses a PowerShell command and its parameters or a script.</param>
    private Collection<PSObject> BuildPipelineAndExecuteStatements(IEnumerable<PowerShellCommandlineStatement> commandLineStatements)
    {
        // Each iteration will pipe the output of the previous statement to the next statement.
        foreach (var statement in commandLineStatements)
        {
            AddStatementToCommandLine(statement);
        }

        return _powerShellSession.Invoke();
    }

    /// <summary>
    /// Executes every PowerShell command line statement one at a time. The output from the
    /// previous statement is not used as input for the next statement.
    /// </summary>
    /// <param name="commandLineStatements"> A list of statements that houses a PowerShell command and its parameters or a script.</param>
    private Collection<PSObject> ExecuteIndividualStatements(IEnumerable<PowerShellCommandlineStatement> commandLineStatements, PipeType pipeType)
    {
        Collection<PSObject> result = new();

        foreach (var statement in commandLineStatements)
        {
            AddStatementToCommandLine(statement);
            result = _powerShellSession.Invoke();
            if (_powerShellSession.GetErrorMessages().Length != 0)
            {
                break;
            }

            // Clear the PowerShell commands and streams to ensure that the previous command is not run again.
            if (pipeType != PipeType.DontClearBetweenStatements)
            {
                _powerShellSession.ClearSession();
            }
        }

        return result;
    }

    /// <summary>Adds a command line statement to the PowerShell session.</summary>
    /// <param name="statement"> A statement that houses a PowerShell command and its parameters or a script.</param>
    /// <remarks>
    /// If the statement contains a command, only the command and its parameters will be
    /// added to the session. When the statement does not contain a command, but contains
    /// a script, we add the script to the session.
    /// </remarks>
    private void AddStatementToCommandLine(PowerShellCommandlineStatement statement)
    {
        var isCommandEmptyOrNull = string.IsNullOrEmpty(statement.Command);
        var isScriptEmptyOrNull = string.IsNullOrEmpty(statement.Script);
        var isCommandUsedWithScript = !isCommandEmptyOrNull && !isScriptEmptyOrNull;

        if (isCommandEmptyOrNull && isScriptEmptyOrNull)
        {
            throw new ArgumentException("Both the Command and Script properties were empty or null.");
        }

        if (isCommandUsedWithScript)
        {
            throw new ArgumentException("Command and Script properties were used in the same statement. This is not allowed.");
        }

        // Add the command if its available.
        if (!isCommandEmptyOrNull)
        {
            _powerShellSession.AddCommand(statement.Command);

            // Add the parameters for the command if any.
            if (statement.Parameters.Count != 0)
            {
                _powerShellSession.AddParameters(statement.Parameters);
            }
        }

        // Add a script if its available and command is non-empty.
        if (isCommandEmptyOrNull && !isScriptEmptyOrNull)
        {
            _powerShellSession.AddScript(statement.Script, statement.UseLocalScope);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _log.Debug("Disposing PowerShellService");
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
