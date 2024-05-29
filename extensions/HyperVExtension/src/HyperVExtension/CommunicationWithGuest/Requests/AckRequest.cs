// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to generate response to GetVersion request.
/// </summary>
internal sealed class AckRequest : RequestBase
{
    public AckRequest(string ackRequestId)
        : base("Ack")
    {
        AckRequestId = ackRequestId;
        GenerateJsonData();
    }

    public string AckRequestId { get; set; }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();
        JsonData![nameof(AckRequestId)] = AckRequestId;
    }
}
