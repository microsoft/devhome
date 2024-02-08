// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Base class for requests from the client.
/// JSON payload is converted to request properties.
/// {
///   "RequestId": "DevSetup{10000000-1000-1000-1000-100000000000}",
///   "RequestType": "GetVersion",
///   "Timestamp":"2023-11-21T08:08:58.6287789Z"
///   <request specific data>
/// }
/// </summary>
internal abstract class RequestBase : IHostRequest
{
    public RequestBase(IRequestMessage message, JsonNode jsonData)
    {
        RequestMessage = message;
        JsonData = jsonData;
        RequestId = GetRequiredStringValue(nameof(RequestId));
        RequestType = GetRequiredStringValue(nameof(RequestType));
        Timestamp = GetRequiredDateTimeValue(nameof(Timestamp));
    }

    public virtual bool IsStatusRequest => false;

    public virtual string RequestId { get; }

    public virtual string RequestType { get; }

    public virtual DateTime Timestamp { get; }

    public abstract IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken);

    public IRequestMessage RequestMessage
    {
        get;
    }

    public JsonNode JsonData
    {
        get;
    }

    protected string GetRequiredStringValue(string valueName)
    {
        try
        {
            var value = (string?)JsonData[valueName];
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{valueName} cannot be empty.");
            }

            return value;
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
}
