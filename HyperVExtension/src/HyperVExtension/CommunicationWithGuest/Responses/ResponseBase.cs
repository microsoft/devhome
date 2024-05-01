// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.CommunicationWithGuest;

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
internal class ResponseBase : IGuestResponse
{
    public ResponseBase(IResponseMessage message, JsonNode jsonData)
    {
        ResponseMessage = message;
        JsonData = jsonData;
        RequestId = GetRequiredStringValue(nameof(RequestId));
        RequestType = GetRequiredStringValue(nameof(RequestType));
        ResponseId = GetRequiredStringValue(nameof(ResponseId));
        ResponseType = GetRequiredStringValue(nameof(ResponseType));
        Version = GetRequiredUintValue(nameof(Version));
        Timestamp = GetRequiredDateTimeValue(nameof(Timestamp));
        Status = GetRequiredUintValue(nameof(Status));
        ErrorDescription = GetRequiredStringValue(nameof(ErrorDescription), true);
    }

    public IResponseMessage ResponseMessage
    {
        get;
    }

    public JsonNode JsonData
    {
        get;
    }

    public virtual string RequestId { get; set; }

    public virtual string RequestType { get; set; }

    public virtual string ResponseId { get; set; }

    public virtual string ResponseType { get; set; }

    public virtual uint Status { get; set; }

    public virtual string ErrorDescription { get; set; }

    public virtual uint Version { get; set; }

    public virtual DateTime Timestamp { get; set; }

    protected string GetRequiredStringValue(string valueName, bool emptyIsOk = false)
    {
        try
        {
            var value = (string?)JsonData[valueName];
            var isValid = emptyIsOk ? value != null : !string.IsNullOrEmpty(value);
            if (!isValid)
            {
                throw new ArgumentException($"{valueName} cannot be empty.");
            }

            return value!;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"{valueName} cannot be empty.", ex);
        }
    }

    protected DateTime GetRequiredDateTimeValue(string valueName)
    {
        try
        {
            return (DateTime?)JsonData[valueName] ?? throw new ArgumentException($"{valueName} cannot be empty.");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"{valueName} cannot be empty.", ex);
        }
    }

    protected uint GetRequiredUintValue(string valueName)
    {
        try
        {
            return (uint?)JsonData[valueName] ?? throw new ArgumentException($"{valueName} cannot be empty.");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"{valueName} cannot be empty.", ex);
        }
    }

    protected bool GetRequiredBoolValue(string valueName)
    {
        try
        {
            return (bool?)JsonData[valueName] ?? throw new ArgumentException($"{valueName} cannot be empty.");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"{valueName} cannot be empty.", ex);
        }
    }
}
