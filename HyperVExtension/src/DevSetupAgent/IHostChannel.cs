// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for communication channel between host and guest..
/// </summary>
public interface IHostChannel
{
    Task<IRequestMessage> WaitForMessageAsync(CancellationToken stoppingToken);

    void SendMessageAsync(IResponseMessage responseMessage, CancellationToken stoppingToken);
}
