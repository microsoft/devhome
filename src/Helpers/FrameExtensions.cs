// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.Helpers;

public static class FrameExtensions
{
    public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}
