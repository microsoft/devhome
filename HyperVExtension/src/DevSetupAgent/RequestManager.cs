// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    private readonly Queue<IHostRequest> _requestQueue = new();
    private bool _asyncRequestRunning;

    public RequestManager(IRequestFactory requestFactory, IHostChannel hostChannel)
    {
        _requestFactory = requestFactory;
        _hostChannel = hostChannel;
    }

    private void ProgressHandler(IHostResponse progressResponse, CancellationToken stoppingToken)
    {
        _hostChannel.SendMessageAsync(progressResponse.GetResponseMessage(), stoppingToken);
    }

    public void ProcessRequestMessage(IRequestMessage message, CancellationToken stoppingToken)
    {
        if (!string.IsNullOrEmpty(message.RequestId))
        {
            var requestContext = new RequestContext(message, _hostChannel);
            var request = _requestFactory.CreateRequest(requestContext);
            if (request.IsStatusRequest)
            {
                // Status requests (like GetVersion) execute immediately and return response.
                var response = request.Execute(ProgressHandler, stoppingToken);
                if (response.SendResponse)
                {
                    _hostChannel.SendMessageAsync(response.GetResponseMessage(), stoppingToken);
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
                    var response = new TooManyRequestsResponse(message.RequestId);
                    _hostChannel.SendMessageAsync(response.GetResponseMessage(), stoppingToken);
                    return;
                }

                lock (_requestQueue)
                {
                    // TODO: send response indicating that request is queued.
                    _requestQueue.Enqueue(request);
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
        while (!stoppingToken.IsCancellationRequested)
        {
            IHostRequest request;
            lock (_requestQueue)
            {
                if (_requestQueue.Count == 0)
                {
                    _asyncRequestRunning = false;
                    break;
                }

                request = _requestQueue.Dequeue();
            }

            try
            {
                var response = request.Execute(ProgressHandler, stoppingToken);
                _hostChannel.SendMessageAsync(response.GetResponseMessage(), stoppingToken);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to execute request.");
            }
        }
    }
}
