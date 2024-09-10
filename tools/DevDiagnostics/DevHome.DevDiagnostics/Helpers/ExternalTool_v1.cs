// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.DevDiagnostics.Helpers;

public enum ExternalToolArgType
{
    None,
    ProcessId,
    Hwnd,
}

public partial class ExternalTool_v1 : ObservableObject
{
    public string ID { get; private set; }

    public string Name { get; private set; }

    public string Executable { get; private set; }

    [JsonConverter(typeof(EnumStringConverter<ExternalToolArgType>))]
    public ExternalToolArgType ArgType { get; private set; } = ExternalToolArgType.None;

    public string ArgPrefix { get; private set; }

    public string OtherArgs { get; private set; }

    public bool IsPinned { get; private set; }

    public ExternalTool_v1(
        string name,
        string executable,
        ExternalToolArgType argtype,
        string argprefix = "",
        string otherArgs = "",
        bool isPinned = false)
    {
        Name = name;
        Executable = executable;
        ArgType = argtype;
        ArgPrefix = argprefix;
        OtherArgs = otherArgs;
        IsPinned = isPinned;
        ID = Guid.NewGuid().ToString();
    }
}
