// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for request manager responsible for processing request messages from host.
/// </summary>
public interface IRequestManager
{
    void ProcessRequestMessage(IRequestMessage message, CancellationToken stoppingToken);
}
