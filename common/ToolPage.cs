// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Models;

namespace DevHome.Common;

public abstract class ToolPage : AutoFocusPage
{
    public abstract string ShortName { get; }
}
