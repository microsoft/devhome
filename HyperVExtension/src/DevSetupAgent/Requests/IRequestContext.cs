// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for creating request handler based on request message.
/// </summary>
public interface IRequestContext
{
    IRequestMessage RequestMessage { get; set; }

    IHostChannel HostChannel { get; set; }

    JsonNode? JsonData { get; set; }
}
