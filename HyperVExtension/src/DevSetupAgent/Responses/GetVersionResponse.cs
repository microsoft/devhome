// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class to generate response to GetVersion request.
/// </summary>
internal sealed class GetVersionResponse : ResponseBase
{
    public GetVersionResponse(string requestId)
        : base(requestId)
    {
        GenerateJsonData();
    }

    // TODO: Get version from assembly
    public string Version => "0.0.1";

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();
        JsonData![nameof(Version)] = Version;
    }
}
