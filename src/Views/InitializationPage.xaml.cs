// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DevHome.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InitializationPage : Page
{
    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IWindowsPackageManager _wpm;
    private readonly IThemeSelectorService _themeSelector;
    private readonly WindowsPackageManagerFactory _wingetFactory;

    public InitializationPage(
       ILogger logger,
       ISetupFlowStringResource stringResource,
       IWindowsPackageManager wpm,
       IThemeSelectorService themeSelector,
       WindowsPackageManagerFactory wingetFactory)
    {
        this.InitializeComponent();
        _logger = logger;
        _stringResource = stringResource;
        _wpm = wpm;
        _themeSelector = themeSelector;
        _wingetFactory = wingetFactory;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _wpm.MsStoreCatalog.ConnectAsync();

        var result = await _wpm.MsStoreCatalog.GetPackagesAsync(new HashSet<string>() { "9N6ZFG245K8J" });
        ISetupTask installtask = result.First().CreateInstallTask(_logger, _wpm, _stringResource, _wingetFactory);
        await installtask.Execute();
    }
}
