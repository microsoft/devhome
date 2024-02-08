// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have no request type.
/// It creates an error response JSON to send back to the client.
/// </summary>
internal class ErrorNoTypeResponse : ResponseBase
{
    public ErrorNoTypeResponse(string requestId)
        : base(requestId)
    {
        Status = 0xFFFFFFFF;
        GenerateJsonData();
    }

    public string Error => "Missing Request type.";

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();
        JsonData![nameof(Error)] = Error;
    }
}
