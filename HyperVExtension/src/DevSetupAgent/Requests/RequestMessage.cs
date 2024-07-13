// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Request message data.
/// </summary>
internal struct RequestMessage : IRequestMessage
{
    public string? RequestId { get; set; }

    public string? RequestData { get; set; }
}
