// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DevHome.DevInsights.Helpers;

public class ExternalToolCollection
{
    public int Version { get; set; }

    public ObservableCollection<ExternalTool> ExternalTools { get; set; }

    public ExternalToolCollection()
    {
        Version = ExternalToolsHelper.ToolsCollectionVersion;
        ExternalTools = [];
    }

    public ExternalToolCollection(int version, ObservableCollection<ExternalTool> tools)
    {
        Version = version;
        ExternalTools = tools;
    }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ExternalToolCollection))]
internal sealed partial class ExternalToolCollectionSourceGenerationContext : JsonSerializerContext
{
}
