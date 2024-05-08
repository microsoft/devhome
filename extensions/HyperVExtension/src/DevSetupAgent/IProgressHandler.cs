// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface to report request execution progress.
/// </summary>
public interface IProgressHandler
{
    public void Progress(IHostResponse progressResponse, CancellationToken stoppingToken);
}
