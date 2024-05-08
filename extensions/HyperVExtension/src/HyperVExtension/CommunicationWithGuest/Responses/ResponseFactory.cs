// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Serilog;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Factory class for creating request handler based on request message.
/// </summary>
public class ResponseFactory : IResponseFactory
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ResponseFactory));

    public delegate IGuestResponse CreateRequestDelegate(IResponseMessage message, JsonNode json);

    private static readonly Dictionary<(string, string), CreateRequestDelegate> _responseFactories = new()
    {
        // TODO: Define request type constants in one place
        { ("Completed", GetVersionRequest.RequestTypeId), (message, json) => new GetVersionResponse(message, json) },
        { ("Completed", GetStateRequest.RequestTypeId), (message, json) => new GetStateResponse(message, json) },
        { ("Completed", ConfigureRequest.RequestTypeId), (message, json) => new ConfigureResponse(message, json) },
        { ("Progress", ConfigureRequest.RequestTypeId), (message, json) => new ConfigureProgressResponse(message, json) },
        { ("Completed", IsUserLoggedInRequest.RequestTypeId), (message, json) => new IsUserLoggedInResponse(message, json) },
    };

    public ResponseFactory()
    {
    }

    public IGuestResponse CreateResponse(IResponseMessage message)
    {
        // Parse message.RequestData and create appropriate request object
        try
        {
            if (!string.IsNullOrEmpty(message.ResponseData))
            {
                _log.Information($"Received message: ID: '{message.CommunicationId}' Data: '{message.ResponseData}'");
                var responseJson = JsonNode.Parse(message.ResponseData);
                var responseType = (string?)responseJson?["ResponseType"];
                var requestType = (string?)responseJson?["RequestType"];
                if ((responseType != null) && (requestType != null))
                {
                    if (_responseFactories.TryGetValue((responseType!, requestType!), out var createResponse))
                    {
                        try
                        {
                            return createResponse(message, responseJson!);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Received unsupported message ID: '{message.CommunicationId}', Message: '{message.ResponseData}'");
                            return new ErrorUnsupportedResponse(message, responseJson!);
                        }
                    }
                    else
                    {
                        return new ErrorUnsupportedResponse(message, responseJson!);
                    }
                }

                _log.Error($"Received message with empty Response or Request type: ID: '{message.CommunicationId}', Message: '{message.ResponseData}'");
                return new ErrorNoTypeResponse(message);
            }
            else
            {
                // We have message id but no data, log error. Send error response.
                _log.Error($"Received message with empty data: ID: {message.CommunicationId}");
                return new ErrorResponse(message);
            }
        }
        catch (Exception ex)
        {
            var messageId = message?.CommunicationId ?? "<unknown>";
            var responseData = message?.ResponseData ?? "<unknown>";
            _log.Error(ex, $"Error processing message. Message ID: {messageId}. Request data: {responseData}");
            return new ErrorResponse(message!);
        }
    }
}
