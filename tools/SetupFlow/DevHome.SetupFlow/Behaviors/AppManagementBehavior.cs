// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Views;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace DevHome.SetupFlow.Behaviors;
public class AppManagementBehavior : Behavior<AppManagementView>
{
    private static AppManagementBehavior _instance;

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
