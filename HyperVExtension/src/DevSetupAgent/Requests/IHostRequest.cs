// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

    uint Version { get; set; }

    DateTime Timestamp { get; }

    IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken);
}
