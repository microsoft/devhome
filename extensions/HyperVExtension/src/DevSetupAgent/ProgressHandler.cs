// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Helper progress handler class to report request execution progress.
/// </summary>
internal sealed class ProgressHandler : IProgressHandler
{
    private readonly IHostChannel _hostChannel;
    private readonly string _communicationId;
    private uint _progressCounter;

    public ProgressHandler(IHostChannel hostChannel, string communicationId)
    {
        _hostChannel = hostChannel;
        _communicationId = communicationId;
    }

    public void Progress(IHostResponse progressResponse, CancellationToken stoppingToken)
    {
        var progressCommunicationId = _communicationId + $"_Progress_{++_progressCounter}";
        var responseMessage = new ResponseMessage(progressCommunicationId, progressResponse.GetResponseData());
        _hostChannel.SendMessageAsync(responseMessage, stoppingToken);
    }
}
