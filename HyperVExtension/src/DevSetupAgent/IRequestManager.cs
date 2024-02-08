// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using HyperVExtension.DevSetupAgent;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for request manager responsible for processing request messages from host.
/// </summary>
public interface IRequestManager
{
    void ProcessRequestMessage(IRequestMessage message, CancellationToken stoppingToken);
}
