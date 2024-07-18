// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace HyperVExtension.Models;

/// <summary>
/// Class that can contain a PowerShell command and its parameters or a PowerShell script.
/// </summary>
public class PowerShellCommandlineStatement
{
    /// <summary> Gets or sets the command to use in the PowerShell command line statement. </summary>
    /// <remarks>
    /// Should not be used in the same <see cref="PowerShellCommandlineStatement"/> object
    /// when <see cref="PowerShellCommandlineStatement.Script"/> is non-empty. This could
    /// lead to undesired results.
    /// </remarks>
    public string Command { get; set; } = string.Empty;

    /// <summary> Gets or sets the parameters for the PowerShell command. </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the script for the PowerShell command line statement.
    /// </summary>
    /// <remarks>
    /// Should not be used in the same <see cref="PowerShellCommandlineStatement"/> object
    /// when <see cref="PowerShellCommandlineStatement.Command"/> is non-empty. This could
    /// lead to undesired results.
    /// </remarks>
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use local scope for the PowerShell script.
    /// </summary>
    public bool UseLocalScope { get; set; } = true;

    /// <summary> Returns a clone of the PowerShellCommandlineStatement object.</summary>
    public PowerShellCommandlineStatement Clone()
    {
        return new PowerShellCommandlineStatement
        {
            Command = Command,
            Parameters = Parameters,
            Script = Script,
            UseLocalScope = UseLocalScope,
        };
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Command: {Command}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Parameters: {CreateParameterString()}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Script: {Script}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"UseLocalScope: {UseLocalScope}");

        return builder.ToString();
    }

    private string CreateParameterString()
    {
        StringBuilder builder = new();
        foreach (var parameter in Parameters)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"{parameter.Key} = {parameter.Value}");
        }

        return builder.ToString();
    }
}
