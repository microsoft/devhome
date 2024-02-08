// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Request message data.
/// </summary>
internal struct RequestMessage : IRequestMessage
{
    public string? RequestId { get; set; }

    public string? RequestData { get; set; }
}
