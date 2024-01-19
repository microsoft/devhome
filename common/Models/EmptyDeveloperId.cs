// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
