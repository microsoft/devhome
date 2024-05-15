// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Base class for responses to the client.
/// JSON payload is generated in GenerateJsonData virtual method.
/// {
///   "RequestId": "DevSetup{10000000-1000-1000-1000-100000000000}",
///   "RequestType": "GetVersion",
///   "Timestamp":"2023-11-21T08:08:58.6287789Z",
///   "Version": 1,
///   <request specific data>
/// }
/// </summary>
internal class RequestBase : IHostRequest
{
    public RequestBase(string requestType)
    {
        Version = 1; // Update version when the response format changes and needs special handling based on version.
        RequestId = $"DevSetup{{{Guid.NewGuid()}}}";
        RequestType = requestType;
        Timestamp = DateTime.UtcNow;
    }

    public virtual string RequestId { get; set; }

    public virtual string RequestType { get; set; }

    public virtual uint Version { get; set; }

    public virtual DateTime Timestamp { get; set; }

    public virtual IRequestMessage GetRequestMessage()
    {
        if (JsonData == null)
        {
            GenerateJsonData();
        }

        return new RequestMessage(RequestId, JsonData!.ToJsonString());
    }

    protected JsonNode? JsonData { get; private set; }

    protected virtual void GenerateJsonData()
    {
        var jsonData = new JsonObject
        {
            [nameof(Version)] = Version,
            [nameof(RequestId)] = RequestId,
            [nameof(RequestType)] = RequestType,
            [nameof(Timestamp)] = Timestamp,
        };
        JsonData = jsonData;
    }
}
