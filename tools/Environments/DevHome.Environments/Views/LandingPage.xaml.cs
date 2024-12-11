// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Environments.Views;

public sealed partial class LandingPage : ToolPage
{
    public LandingPageViewModel ViewModel { get; }

    public LandingPage()
    {
        ViewModel = Application.Current.GetService<LandingPageViewModel>();
        InitializeComponent();
        ViewModel.Initialize(NotificationQueue);
    }
}
