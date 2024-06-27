// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.HostGuestCommunication;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to translate request/response objects to/from messages and pass them to/from channel to guest VM..
/// </summary>
internal sealed class GuestKvpSession : IDisposable
{
    private static uint _nextCommunicationIdCounter = 1;
    private readonly Guid _vmId;
    private readonly GuestKvpChannel _channel;
    private readonly ResponseFactory _responseFactory = new();
    private HashSet<string> _processedMessages = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public GuestKvpSession(Guid vmId)
    {
        _vmId = vmId;
        _channel = new GuestKvpChannel(vmId);
    }

    public uint SendRequest(IHostRequest request, CancellationToken stoppingToken)
    {
        var communicationIdCounter = _nextCommunicationIdCounter++;
        _channel.SendMessage(request.GetRequestMessage(), communicationIdCounter, stoppingToken);
        return communicationIdCounter;
    }

    public List<IGuestResponse> WaitForResponse(uint communicationIdCounter, string requestId, TimeSpan timeout, bool expectProgressResponse, CancellationToken stoppingToken)
    {
        var communicationId = $"{MessageHelper.DevSetupPrefix}{{{communicationIdCounter}}}";
        var result = new List<IGuestResponse>();
        var responseMessages = _channel.WaitForResponseMessages(communicationId, timeout, expectProgressResponse, stoppingToken);

        // There is no way for host to remove messages from guest kvp. So, we need to keep track of processed messages.
        // If we find that we received the same message as in previous call of this method, we will ignore it.
        // Host will send "AckRequest" to let guest know that it can remove the message from kvp.
        var newProcessedMessages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var responseMessage in responseMessages)
        {
            if (!_processedMessages.Contains(responseMessage.CommunicationId))
            {
                var response = _responseFactory.CreateResponse(responseMessage);
                if (requestId.Equals(response.RequestId, StringComparison.OrdinalIgnoreCase))
                {
                    newProcessedMessages.Add(responseMessage.CommunicationId);

                    result.Add(response);
                    _channel.SendMessage(new AckRequest(responseMessage.CommunicationId).GetRequestMessage(), _nextCommunicationIdCounter++, stoppingToken);

                    // We've received response to request with communicationId, so we can remove kvp entries for
                    // this communicationId as they've been processed on Hyper-V side.
                    _channel.CleanUp(communicationId);
                }
            }
            else
            {
                // We've already processed this message in previous call of this method, but it was not deleted yet
                // on Hyper-V side, so keep it in processed messages list.
                newProcessedMessages.Add(responseMessage.CommunicationId);
            }
        }

        _processedMessages = newProcessedMessages;
        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel?.Dispose();
            }

            _disposed = true;
        }
    }

    internal void SetNextCommunicationIdCounter(uint communicationIdCounter)
    {
        if (communicationIdCounter > _nextCommunicationIdCounter)
        {
            _nextCommunicationIdCounter = communicationIdCounter;
        }
    }
}
