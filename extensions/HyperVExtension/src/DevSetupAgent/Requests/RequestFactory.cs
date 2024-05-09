// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Serilog;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Factory class for creating request handler based on request message.
/// </summary>
public class RequestFactory : IRequestFactory
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RequestFactory));

    public delegate IHostRequest CreateRequestDelegate(IRequestContext requestContext);

    private static readonly Dictionary<string, CreateRequestDelegate> _requestFactories = new()
    {
        { GetVersionRequest.RequestTypeId, (requestContext) => new GetVersionRequest(requestContext) },
        { GetStateRequest.RequestTypeId, (requestContext) => new GetStateRequest(requestContext) },
        { ConfigureRequest.RequestTypeId, (requestContext) => new ConfigureRequest(requestContext) },
        { AckRequest.RequestTypeId, (requestContext) => new AckRequest(requestContext) },
        { IsUserLoggedInRequest.RequestTypeId, (requestContext) => new IsUserLoggedInRequest(requestContext) },
    };

    public RequestFactory()
    {
    }

    public IHostRequest CreateRequest(IRequestContext requestContext)
    {
        // Parse message.RequestData and create appropriate request object
        try
        {
            if (!string.IsNullOrEmpty(requestContext.RequestMessage.RequestData))
            {
                _log.Information($"Received message: ID: '{requestContext.RequestMessage.CommunicationId}' Data: '{requestContext.RequestMessage.RequestData}'");
                var requestJson = JsonNode.Parse(requestContext.RequestMessage.RequestData);
                var requestType = (string?)requestJson?["RequestType"];
                if (requestType != null)
                {
                    requestContext.JsonData = requestJson!;
                    if (_requestFactories.TryGetValue(requestType, out var createRequest))
                    {
                        return createRequest(requestContext);
                    }
                    else
                    {
                        return new ErrorUnsupportedRequest(requestContext);
                    }
                }

                return new ErrorNoTypeRequest(requestContext.RequestMessage);
            }
            else
            {
                // We have message id but no data, log error. Send error response.
                _log.Information($"Received message with empty data: ID: {requestContext.RequestMessage.CommunicationId}");
                return new ErrorRequest(requestContext.RequestMessage);
            }
        }
        catch (Exception ex)
        {
            var messageId = requestContext.RequestMessage.CommunicationId ?? "<unknown>";
            var requestData = requestContext.RequestMessage.RequestData ?? "<unknown>";
            _log.Error(ex, $"Error processing message. Message ID: {messageId}. Request data: {requestData}");
            return new ErrorRequest(requestContext.RequestMessage);
        }
    }
}
