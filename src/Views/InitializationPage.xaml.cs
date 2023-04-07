// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Management.Automation;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using DevHome.ViewModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace DevHome.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InitializationPage : Page
{
    public InitializationViewModel ViewModel
    {
        get;
    }

    public InitializationPage(InitializationViewModel initializationViewModel)
    {
        this.InitializeComponent();
        ViewModel = initializationViewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.OnPageLoadedAsync();
    }
}
