// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common;

public abstract class ToolPage : Page
{
    public abstract string ShortName { get; }
}
