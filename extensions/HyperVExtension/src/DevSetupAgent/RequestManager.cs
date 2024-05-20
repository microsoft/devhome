// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.HostGuestCommunication;
using Serilog;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Request manager responsible for processing request messages from host.
/// </summary>
public class RequestManager : IRequestManager
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RequestManager));
    private const uint MaxRequestQueueSize = 3;
    private readonly IRequestFactory _requestFactory;
    private readonly IHostChannel _hostChannel;
    private readonly Queue<(string communicationId, IHostRequest request)> _requestQueue = new();
    private bool _asyncRequestRunning;
    private string? _currentAsyncRequestCommunicationId;
    private IHostRequest? _currentAsyncRequest;

    public RequestManager(IRequestFactory requestFactory, IHostChannel hostChannel)
    {
        _requestFactory = requestFactory;
        _hostChannel = hostChannel;
    }

    public void ProcessRequestMessage(IRequestMessage message, CancellationToken stoppingToken)
    {
        if (!string.IsNullOrEmpty(message.CommunicationId))
        {
            var requestsInQueue = new List<RequestsInQueue>();

            lock (_requestQueue)
            {
                // Get a snapshot of the current requests in the queue.
                // _currentAsyncRequestCommunicationId is not in the waiting queue anymore, but it doesn't matter for the host.
                // We'll report all requests that we have at the moment. By the time host will receive the response, the current requests
                // could be finished. This is not intended to be super accurate, but to give host an idea of what's going on, so it can wait
                // for an idle state before sending another request.
                if (!string.IsNullOrEmpty(_currentAsyncRequestCommunicationId) && (_currentAsyncRequest != null))
                {
                    requestsInQueue.Add(new RequestsInQueue(_currentAsyncRequestCommunicationId, _currentAsyncRequest.RequestId));
                }

                requestsInQueue.AddRange(_requestQueue.Select(item => new RequestsInQueue(item.communicationId, item.request.RequestId)).ToList());
            }

            var requestContext = new RequestContext(message, _hostChannel, requestsInQueue);
            var request = _requestFactory.CreateRequest(requestContext);
            if (request.IsStatusRequest)
            {
                // Status requests (like GetVersion) execute immediately and return response.
                var response = request.Execute(new ProgressHandler(_hostChannel, message.CommunicationId), stoppingToken);
                if (response.SendResponse)
                {
                    _hostChannel.SendMessageAsync(new ResponseMessage(message.CommunicationId, response.GetResponseData()), stoppingToken);
                }
            }
            else
            {
                // Non-status request are queued and executed async in order one at a time.
                int queueCount;
                lock (_requestQueue)
                {
                    queueCount = _requestQueue.Count;
                }

                if (queueCount > MaxRequestQueueSize)
                {
                    _log.Error($"Too many requests.");
                    var responseData = new TooManyRequestsResponse(request.RequestId).GetResponseData();
                    _hostChannel.SendMessageAsync(new ResponseMessage(message.CommunicationId, responseData), stoppingToken);
                    return;
                }

                lock (_requestQueue)
                {
                    // TODO: send response indicating that request is queued.
                    _requestQueue.Enqueue((message.CommunicationId, request));
                    if (!_asyncRequestRunning)
                    {
                        _asyncRequestRunning = true;
                        Task.Run(() => ProcessRequestQueue(stoppingToken), stoppingToken);
                    }
                }
            }
        }
        else
        {
            // Shouldn't happen, Log error
            _log.Error($"Received empty message.");
        }
    }

    private void ProcessRequestQueue(CancellationToken stoppingToken)
    {
        while (true)
        {
            lock (_requestQueue)
            {
                if ((_requestQueue.Count == 0) || stoppingToken.IsCancellationRequested)
                {
                    _asyncRequestRunning = false;
                    _currentAsyncRequestCommunicationId = null;
                    _currentAsyncRequest = null;
                    break;
                }

                (_currentAsyncRequestCommunicationId, _currentAsyncRequest) = _requestQueue.Dequeue();
            }

            try
            {
                var response = _currentAsyncRequest.Execute(
                    new ProgressHandler(_hostChannel, _currentAsyncRequestCommunicationId),
                    stoppingToken);

                _hostChannel.SendMessageAsync(new ResponseMessage(_currentAsyncRequestCommunicationId, response.GetResponseData()), stoppingToken);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to execute request.");
            }
        }
    }
}
