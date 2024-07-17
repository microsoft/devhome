// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using HyperVExtension.Providers;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Factory class for creating request handler based on request message.
/// </summary>
public class ResponseFactory : IResponseFactory
{
    public delegate IGuestResponse CreateRequestDelegate(IResponseMessage message, JsonNode json);

    private static readonly Dictionary<(string, string), CreateRequestDelegate> _responseFactories = new()
    {
        // TODO: Define request type constants in one place
        { ("Completed", "GetVersion"), (message, json) => new GetVersionResponse(message, json) },
        { ("Completed", "Configure"), (message, json) => new ConfigureResponse(message, json) },
        { ("Progress", "Configure"), (message, json) => new ConfigureProgressResponse(message, json) },
        { ("Completed", "IsUserLoggedIn"), (message, json) => new IsUserLoggedInResponse(message, json) },
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
                Logging.Logger()?.ReportInfo($"Received message: ID: '{message.ResponseId}' Data: '{message.ResponseData}'");
                var responseJson = JsonNode.Parse(message.ResponseData);
                var responseType = (string?)responseJson?["ResponseType"];
                var requestType = (string?)responseJson?["RequestType"];
                if ((responseType != null) && (requestType != null))
                {
                    if (_responseFactories.TryGetValue((responseType!, requestType!), out var createResponse))
                    {
                        // TODO: Try/catch error.
                        return createResponse(message, responseJson!);
                    }
                    else
                    {
                        return new ErrorUnsupportedResponse(message, responseJson!);
                    }
                }

                Logging.Logger()?.ReportInfo($"Received message with empty Response or Request type: ID: '{message.ResponseId}', Message: '{message.ResponseData}'");
                return new ErrorNoTypeResponse(message);
            }
            else
            {
                // We have message id but no data, log error. Send error response.
                Logging.Logger()?.ReportInfo($"Received message with empty data: ID: {message.ResponseId}");
                return new ErrorResponse(message);
            }
        }
        catch (Exception ex)
        {
            var messageId = message?.ResponseId ?? "<unknown>";
            var responseData = message?.ResponseData ?? "<unknown>";
            Logging.Logger()?.ReportError($"Error processing message. Message ID: {messageId}. Request data: {responseData}", ex);
            return new ErrorResponse(message!);
        }
    }
}
