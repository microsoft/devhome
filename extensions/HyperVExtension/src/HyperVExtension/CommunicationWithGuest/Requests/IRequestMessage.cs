// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Interface for providing request message data.
/// </summary>
public interface IRequestMessage
{
    string RequestId { get; set; }

    string RequestData { get; set; }
}
