// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Storage;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Factory class for creating request handler based on request message.
/// </summary>
public class RequestFactory : IRequestFactory
{
    public delegate IHostRequest CreateRequestDelegate(IRequestMessage message, JsonNode json);

    private static readonly Dictionary<string, CreateRequestDelegate> _requestFactories = new ()
    {
        // TODO: Define request type constants in one place
        { "GetVersion", (message, json) => new GetVersionRequest(message, json) },
        { "Configure", (message, json) => new ConfigureRequest(message, json) },
    };

    public RequestFactory()
    {
    }

    public IHostRequest CreateRequest(IRequestMessage message)
    {
        // Parse message.RequestData and create appropriate request object
        try
        {
            if (!string.IsNullOrEmpty(message.RequestData))
            {
                Logging.Logger()?.ReportInfo($"Received message: ID: '{message.RequestId}' Data: '{message.RequestData}'");
                var requestJson = JsonNode.Parse(message.RequestData);
                var requestType = (string?)requestJson?["RequestType"];
                if (requestType != null)
                {
                    if (_requestFactories.TryGetValue(requestType, out var createRequest))
                    {
                        // TODO: Try/catch error.
                        return createRequest(message, requestJson!);
                    }
                    else
                    {
                        return new ErrorUnsupportedRequest(message, requestJson!, requestType);
                    }
                }

                return new ErrorNoTypeRequest(message, requestJson!);
            }
            else
            {
                // We have message id but no data, log error. Send error response.
                Logging.Logger()?.ReportInfo($"Received message with empty data: ID: {message.RequestId}");
                return new ErrorRequest(message, new JsonObject());
            }
        }
        catch (Exception ex)
        {
            var messageId = message?.RequestId ?? "<unknown>";
            var requestData = message?.RequestData ?? "<unknown>";
            Logging.Logger()?.ReportError($"Error processing message. Message ID: {messageId}. Request data: {requestData}", ex);
            return new ErrorRequest(message!, new JsonObject());
        }
    }
}
