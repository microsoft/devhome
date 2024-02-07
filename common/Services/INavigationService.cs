// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.Common.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack
    {
        get;
    }

    // Used to pass data between view models during a navigation
    object? LastParameterUsed
    {
        get;
    }

    Frame? Frame
    {
        get; set;
    }

    string DefaultPage
    {
        get; set;
    }

    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    bool GoBack();

    bool GoForward();
}
