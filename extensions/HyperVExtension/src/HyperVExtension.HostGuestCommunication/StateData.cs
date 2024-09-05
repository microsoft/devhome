// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace HyperVExtension.HostGuestCommunication;

// Helper class to return DevSetupAgent state to the client.
public class StateData
{
    public StateData(List<RequestsInQueue> requestsInQueue)
    {
        RequestsInQueue = requestsInQueue;
    }

    public List<RequestsInQueue> RequestsInQueue { get; private set; }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(StateData))]
public sealed partial class StateDataSourceGenerationContext : JsonSerializerContext
{
}
