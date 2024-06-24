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
///   "Version": "1",
///   <request specific data>
/// }
/// </summary>
internal class ResponseBase : IHostResponse
{
    public ResponseBase(string requestId, string? requestType = null)
    {
        RequestId = requestId;
        ResponseId = requestId;
        Status = Windows.Win32.Foundation.HRESULT.S_OK;
        Version = 1; // Update version when the response format changes and needs special handling based on version.
        RequestType = requestType != null ? requestType : "Unknown";
        ResponseType = "Completed";
        Timestamp = DateTime.UtcNow;
        ErrorDescription = string.Empty;
        SendResponse = true;
    }

    public virtual string RequestId { get; set; }

    public virtual string RequestType { get; set; }

    public virtual string ResponseId { get; set; }

    public virtual string ResponseType { get; set; }

    public virtual uint Status { get; set; }

    public virtual string ErrorDescription { get; set; }

    public virtual uint Version { get; set; }

    public virtual DateTime Timestamp { get; set; }

    public virtual string GetResponseData()
    {
        if (JsonData == null)
        {
            GenerateJsonData();
        }

        return JsonData!.ToJsonString();
    }

    public virtual bool SendResponse { get; set; }

    protected JsonNode? JsonData { get; private set; }

    protected virtual void GenerateJsonData()
    {
        var jsonData = new JsonObject
        {
            [nameof(Version)] = Version,
            [nameof(RequestId)] = RequestId,
            [nameof(RequestType)] = RequestType,
            [nameof(ResponseId)] = ResponseId,
            [nameof(ResponseType)] = ResponseType,
            [nameof(Status)] = Status,
            [nameof(ErrorDescription)] = ErrorDescription,
            [nameof(Timestamp)] = Timestamp,
        };
        JsonData = jsonData;
    }
}
