// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Base class for responses to the client.
/// JSON payload is generated in GenerateJsonData virtual method.
/// {
///   "ResponseId": "DevSetup{10000000-1000-1000-1000-100000000000}",
///   "ResponseType": "GetVersion",
///   "Timestamp":"2023-11-21T08:08:58.6287789Z",
///   "Version": "0.0.1",
///   <request specific data>
/// }
/// </summary>
internal class ResponseBase : IHostResponse
{
    public ResponseBase(string requestId)
    {
        RequestId = requestId;
        ResponseId = requestId;
        Status = Windows.Win32.Foundation.HRESULT.S_OK;
        RequestType = "Unknown";
        ResponseType = "Completed";
        Timestamp = DateTime.UtcNow;
    }

    public string RequestId { get; set; }

    public string RequestType { get; set; }

    public string ResponseId { get; set; }

    public string ResponseType { get; set; }

    public uint Status { get; set; }

    public DateTime Timestamp { get; set; }

    public IResponseMessage GetResponseMessage()
    {
        if (JsonData == null)
        {
            GenerateJsonData();
        }

        return new ResponseMessage(ResponseId, JsonData!.ToJsonString());
    }

    protected JsonNode? JsonData { get; private set; }

    protected virtual void GenerateJsonData()
    {
        var jsonData = new JsonObject
        {
            [nameof(RequestId)] = RequestId,
            [nameof(ResponseId)] = ResponseId,
            [nameof(ResponseType)] = ResponseType,
            [nameof(Status)] = Status,
            [nameof(Timestamp)] = Timestamp,
        };
        JsonData = jsonData;
    }
}
