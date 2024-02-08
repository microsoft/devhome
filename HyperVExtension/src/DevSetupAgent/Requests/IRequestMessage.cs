// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for providing request message data.
/// </summary>
public interface IRequestMessage
{
    string? RequestId { get; set; }

    string? RequestData { get; set; }
}
