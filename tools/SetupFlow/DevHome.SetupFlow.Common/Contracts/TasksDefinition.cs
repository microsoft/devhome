// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class TasksDefinition
{
    public List<InstallTaskDefinition> Install
    {
        get; set;
    }

    public DevDriveTaskDefinition DevDrive
    {
        get; set;
    }

    public ConfigurationTaskDefinition Configuration
    {
        get; set;
    }

    public string ToJsonString()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        var jsonString = JsonSerializer.Serialize(this, options);
        return JsonSerializer.Serialize<string>(jsonString, options);
    }

    public static TasksDefinition FromJsonString(string tasksDefinition)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        var definition = JsonSerializer.Deserialize<TasksDefinition>(tasksDefinition, options);
        if (definition?.Configuration != null)
        {
            definition.Configuration.FilePath = JsonSerializer.Deserialize<string>($"\"{definition.Configuration.FilePath}\"");
            definition.Configuration.Content = JsonSerializer.Deserialize<string>($"\"{definition.Configuration.Content}\"");
        }

        return definition;
    }
}
