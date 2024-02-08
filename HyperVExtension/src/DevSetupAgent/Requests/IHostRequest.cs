// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.DevSetupAgent;

public delegate void ProgressHandler(IHostResponse progressResponse, CancellationToken stoppingToken);

/// <summary>
/// Interface for handling requests from client (host machine).
/// </summary>
public interface IHostRequest
{
    bool IsStatusRequest { get; }

    string RequestId { get; }

    string RequestType { get; }

    DateTime Timestamp { get; }

    IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken);
}
