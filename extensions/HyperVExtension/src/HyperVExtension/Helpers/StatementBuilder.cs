// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;

namespace HyperVExtension.Helpers;

/// <summary>
/// Helper class to allow PowerShell Command line statements to be built easier.
/// </summary>
public class StatementBuilder
{
    private readonly List<PowerShellCommandlineStatement> _powerShellCommandLineStatements = new();

    /// <summary> Adds a new PowerShell commandline statement with only a single command inside.</summary>
    /// <returns>The StatementBuilder object that is being used to contain the commandline statements</returns>
    public StatementBuilder AddCommand(string command)
    {
        var statement = new PowerShellCommandlineStatement() { Command = command, };
        _powerShellCommandLineStatements.Add(statement);
        return this;
    }

    /// <summary> Adds a new parameter to the last PowerShell commandline statement that was created.</summary>
    /// <returns>The StatementBuilder object that is being used to contain the commandline statements</returns>
    public StatementBuilder AddParameter(string propertyName, object propertyValue)
    {
        var statement = _powerShellCommandLineStatements.LastOrDefault();

        if (statement == null)
        {
            throw new ArgumentException($"Cannot add parameter to the last {nameof(PowerShellCommandlineStatement)} because it was null");
        }

        statement.Parameters.Add(propertyName, propertyValue);
        return this;
    }

    /// <summary>  Adds a new PowerShell commandline statement with only a single script inside.</summary>
    /// <returns>The StatementBuilder object that is being used to contain the commandline statements</returns>
    public StatementBuilder AddScript(string script, bool useLocalScope)
    {
        var statement = new PowerShellCommandlineStatement() { Script = script, UseLocalScope = useLocalScope };
        _powerShellCommandLineStatements.Add(statement);
        return this;
    }

    /// <summary>
    /// Signals to the StatementBuilder to provide a copy of its internal list of PowerShell commandline statements.
    /// Note: Doing this this will also clear StatementBuilder's internal list after it returns the copy.
    /// </summary>
    /// <returns> A copy of the StatementBuilder's internal list which contains the commandline statements</returns>
    public List<PowerShellCommandlineStatement> Build()
    {
        var statementListClone = new List<PowerShellCommandlineStatement>();
        foreach (var statement in _powerShellCommandLineStatements)
        {
            statementListClone.Add(statement.Clone());
        }

        Reset();
        return statementListClone;
    }

    /// <summary> Clears the StatementBuilder's internal list of PowerShell statements.</summary>
    public void Reset()
    {
        _powerShellCommandLineStatements.Clear();
    }
}
