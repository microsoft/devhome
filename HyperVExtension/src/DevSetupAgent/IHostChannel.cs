// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for communication channel between host and guest..
/// </summary>
public interface IHostChannel
{
    Task<IRequestMessage> WaitForMessageAsync(CancellationToken stoppingToken);

    void SendMessageAsync(IResponseMessage responseMessage, CancellationToken stoppingToken);

    void DeleteResponseMessageAsync(string responseId, CancellationToken stoppingToken);
}
