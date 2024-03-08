// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    public RequestBase(IRequestContext requestContext)
    {
        RequestContext = requestContext;
        RequestId = GetRequiredStringValue(nameof(RequestId));
        RequestType = GetRequiredStringValue(nameof(RequestType));
        Version = GetRequiredUintValue(nameof(Version));
        Timestamp = GetRequiredDateTimeValue(nameof(Timestamp));
    }

    protected IRequestContext RequestContext { get; }

    public virtual bool IsStatusRequest => false;

    public virtual string RequestId { get; }

    public virtual string RequestType { get; }

    public virtual uint Version { get; set; }

    public virtual DateTime Timestamp { get; }

    public abstract IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken);

    public IRequestMessage RequestMessage => RequestContext.RequestMessage;

    public JsonNode JsonData => RequestContext.JsonData!;

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
}
