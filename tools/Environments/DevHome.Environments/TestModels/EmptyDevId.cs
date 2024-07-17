// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.TestModels;

/// <summary>
/// Class to support compute systems providers that don't need a developer ID.
/// </summary>
public class EmptyDevId : IDeveloperId
{
    public string LoginId { get; }

    public string Url { get; }

    public EmptyDevId()
    {
        LoginId = string.Empty;
        Url = string.Empty;
    }
}
