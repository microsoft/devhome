// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Models;

/// <summary>
/// Empty implementation of IDeveloperId.
/// </summary>
public class EmptyDeveloperId : IDeveloperId
{
    public string LoginId => string.Empty;

    public string Url => string.Empty;
}
