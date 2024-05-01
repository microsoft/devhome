// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to generate GetState request.
/// </summary>
internal sealed class GetStateRequest : RequestBase
{
    public const string RequestTypeId = "GetState";

    public GetStateRequest()
        : base(RequestTypeId)
    {
        GenerateJsonData();
    }
}
