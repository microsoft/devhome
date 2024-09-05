// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace HyperVExtension.DevSetupAgent;

internal sealed class IsUserLoggedInResponse : ResponseBase
{
    public IsUserLoggedInResponse(string requestId, List<string> loggedInUsers)
        : base(requestId)
    {
        RequestType = "IsUserLoggedIn";

        // Return empty list for now. Reserved for the future use to deal with multiple logged in users.
        LoggedInUsers = new List<string>();
        IsUserLoggedIn = loggedInUsers.Count > 0;
        GenerateJsonData();
    }

    public List<string> LoggedInUsers { get; }

    public bool IsUserLoggedIn { get; }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();

        JsonData![nameof(IsUserLoggedIn)] = IsUserLoggedIn;
        JsonData![nameof(LoggedInUsers)] = JsonSerializer.Serialize(LoggedInUsers, IsUserLoggedInResponseSourceGenerationContext.Default.ListString);
    }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<string>))]
internal sealed partial class IsUserLoggedInResponseSourceGenerationContext : JsonSerializerContext
{
}
