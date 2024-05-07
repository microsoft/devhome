// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for providing request message data.
/// </summary>
public interface IRequestMessage
{
    string? CommunicationId { get; set; }

    string? RequestData { get; set; }
}
