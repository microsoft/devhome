// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Helper class to hold the request context.
/// </summary>
internal sealed class RequestContext : IRequestContext
{
    public RequestContext(IRequestMessage requestMessage, IHostChannel channel)
    {
        RequestMessage = requestMessage;
        HostChannel = channel;
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
}
