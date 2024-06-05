// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Interface for creating response to host request.
/// </summary>
public interface IGuestResponse
{
    uint Version { get; set; }

    string RequestId { get; set; }

    string RequestType { get; set; }

    string ResponseId { get; set; }

    string ResponseType { get; set; }

    uint Status { get; set; }

    string ErrorDescription { get; set; }

    DateTime Timestamp { get; set; }
}
