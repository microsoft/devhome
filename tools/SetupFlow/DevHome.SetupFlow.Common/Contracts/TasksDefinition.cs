// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class TasksDefinition
{
    public List<InstallTaskDefinition> Install
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
        return JsonSerializer.Deserialize<TasksDefinition>(tasksDefinition);
    }
}
