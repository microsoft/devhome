// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.Views;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace DevHome.SetupFlow.Behaviors;

// TODO: Convert this class to a SetupShell behavior class to benefit from customization in all setup flow pages
// https://github.com/microsoft/devhome/issues/1621

/// <summary>
/// Behavior class for <see cref="AppManagementView"/>.
/// </summary>
public class AppManagementBehavior : Behavior<AppManagementView>
{
    private static AppManagementBehavior _instance;

    /// <summary>
    /// Gets the page title
    /// </summary>
    public static string Title => _instance?.AssociatedObject.Title ?? string.Empty;

    protected override void OnAttached()
    {
        base.OnAttached();
        _instance = this;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _instance = null;
    }

    public static void SetHeaderVisibility(bool isVisible)
    {
        _instance?.AssociatedObject.SetHeaderVisibility(isVisible ? Visibility.Visible : Visibility.Collapsed);
    }
}
