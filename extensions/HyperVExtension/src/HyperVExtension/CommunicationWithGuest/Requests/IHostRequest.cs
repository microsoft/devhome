// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Interface for creating response to host request.
/// </summary>
public interface IHostRequest
{
    string RequestId { get; set; }

    string RequestType { get; set; }

    uint Version { get; set; }

    DateTime Timestamp { get; set; }

    IRequestMessage GetRequestMessage();
}
