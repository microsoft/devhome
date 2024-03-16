// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using HyperVExtension.Providers;
using Microsoft.Win32;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class to translate request/response objects to/from messages and pass them to/from channel to guest VM..
/// </summary>
internal sealed class GuestKvpSession : IDisposable
{
    private readonly Guid _vmId;
    private readonly GuestKvpChannel _channel;
    private readonly ResponseFactory _responseFactory = new();
    private Dictionary<string, IResponseMessage> _processedMessages = new();
    private bool _disposed;

    public GuestKvpSession(Guid vmId)
    {
        _vmId = vmId;
        _channel = new GuestKvpChannel(vmId);
    }

    public void SendRequest(IHostRequest request, CancellationToken stoppingToken)
    {
        _channel.SendMessage(request.GetRequestMessage(), stoppingToken);
    }

    public List<IGuestResponse> WaitForResponse(string responseId, TimeSpan timeout, bool expectProgressResponse, CancellationToken stoppingToken)
    {
        var result = new List<IGuestResponse>();
        var responseMessages = _channel.WaitForResponseMessages(responseId, timeout, expectProgressResponse, stoppingToken);

        // There is no way for host to remove messages from guest kvp. So, we need to keep track of processed messages.
        // If we find that we received the same message as in previous call of this method, we will ignore it.
        // Host will send "AckRequest" to let guest know that it can remove the message from kvp.
        var newProcessedMessages = new Dictionary<string, IResponseMessage>();

        foreach (var responseMessage in responseMessages)
        {
            if (!_processedMessages.ContainsKey(responseMessage.ResponseId))
            {
                result.Add(_responseFactory.CreateResponse(responseMessage));
                _channel.SendMessage(new AckRequest(responseMessage.ResponseId).GetRequestMessage(), stoppingToken);
            }

            newProcessedMessages[responseMessage.ResponseId] = responseMessage;
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
}
