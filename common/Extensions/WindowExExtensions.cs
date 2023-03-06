// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace DevHome.Common.Extensions;

/// <summary>
/// This class add extension methods to the <see cref="WindowEx"/> class.
/// </summary>
public static class WindowExExtensions
{
    /// <summary>
    /// Show an error message on the window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="content">Dialog content.</param>
    /// <param name="buttonText">Close button text.</param>
    public static async Task ShowErrorMessageDialogAsync(this WindowEx window, string title, string content, string buttonText)
    {
        await window.ShowMessageDialogAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock()
            {
                Text = content,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords,
            };
            dialog.PrimaryButtonText = buttonText;
        });
    }

    /// <summary>
    /// Generic implementation for creating and displaying a message dialog on
    /// a window.
    ///
    /// This extension method overloads <see cref="WindowEx.ShowMessageDialogAsync"/>.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="action">Action performed on the created dialog.</param>
    private static async Task ShowMessageDialogAsync(this WindowEx window, Action<ContentDialog> action)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = window.Content.XamlRoot,
        };
        action(dialog);
        await dialog.ShowAsync();
    }
}
