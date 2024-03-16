// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to generate response to GetVersion request.
/// </summary>
internal sealed class GetVersionRequest : RequestBase
{
    public GetVersionRequest()
        : base("GetVersion")
    {
        GenerateJsonData();
    }
}
