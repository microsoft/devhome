// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using HyperVExtension.HostGuestCommunication;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Helper class to hold the request context.
/// </summary>
internal sealed class RequestContext : IRequestContext
{
    public RequestContext(
        IRequestMessage requestMessage,
        IHostChannel channel,
        List<RequestsInQueue> requestsInQueue)
    {
        RequestMessage = requestMessage;
        HostChannel = channel;
        RequestsInQueue = requestsInQueue;
    }

    public IRequestMessage RequestMessage
    {
        get; set;
    }

    public IHostChannel HostChannel
    {
        get; set;
    }

    public JsonNode? JsonData
    {
        get; set;
    }

    public List<RequestsInQueue> RequestsInQueue
    {
        get; set;
    }
}
