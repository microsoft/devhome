// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to generate GetVersion request.
/// </summary>
internal sealed class GetVersionRequest : RequestBase
{
    public const string RequestTypeId = "GetVersion";

    public GetVersionRequest()
        : base(RequestTypeId)
    {
        GenerateJsonData();
    }
}
