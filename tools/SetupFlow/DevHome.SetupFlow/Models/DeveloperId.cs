// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Concrete DeveloperId.  Used when a user selects a repo from the URL page
/// because URL page does not require a login.
/// </summary>
public class DeveloperId : IDeveloperId
{
    public string LoginId
    {
        get;
    }

    public string Url
    {
        get;
    }

    public DeveloperId(string loginId, string url)
    {
        LoginId = loginId;
        Url = url;
    }
}
