// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to generate response to Configure request.
/// </summary>
internal sealed class ConfigureRequest : RequestBase
{
    private readonly string _configureYaml;

    public ConfigureRequest(string configureYaml)
        : base("Configure")
    {
        _configureYaml = configureYaml;
        GenerateJsonData();
    }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();

        var noNewLinesYaml = _configureYaml.Replace(System.Environment.NewLine, "\\n");
        JsonData!["Configure"] = noNewLinesYaml;
    }
}
