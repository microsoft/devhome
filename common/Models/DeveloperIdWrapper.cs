// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Models;

/// <summary>
/// Wrapper class for a IDeveloperId. It cache the LoginId and Url.
/// </summary>
public class DeveloperIdWrapper
{
    public DeveloperIdWrapper(IDeveloperId developerId)
    {
        LoginId = developerId.LoginId;
        Url = developerId.Url;
        DeveloperId = developerId;
    }

    public string LoginId { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    // use this directly with caution. Only use when needing to access the
    // the original IDeveloperID object. E.g to use with calls to an extensions method.
    public IDeveloperId DeveloperId { get; }
}
