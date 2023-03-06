// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Newtonsoft.Json;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DevHome.Helpers;

internal class NavConfig
{
    [JsonProperty("navMenu")]
    public NavMenu NavMenu { get; set; }
}

internal class NavMenu
{
    [JsonProperty("groups")]
    public Group[] Groups { get; set; }
}

internal class Group
{
    [JsonProperty("identity")]
    public string Identity { get; set; }

    [JsonProperty("tools")]
    public Tool[] Tools { get; set; }
}

internal class Tool
{
    [JsonProperty("identity")]
    public string Identity { get; set; }

    [JsonProperty("assembly")]
    public string Assembly { get; set; }

    [JsonProperty("viewFullName")]
    public string ViewFullName { get; set; }

    [JsonProperty("viewModelFullName")]
    public string ViewModelFullName { get; set; }
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
