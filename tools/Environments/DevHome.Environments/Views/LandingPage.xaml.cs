// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadModelAsync(false);
    }
}
