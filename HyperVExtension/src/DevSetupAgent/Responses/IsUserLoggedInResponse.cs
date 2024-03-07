// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

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
        JsonData![nameof(LoggedInUsers)] = JsonSerializer.Serialize(LoggedInUsers);
    }
}
