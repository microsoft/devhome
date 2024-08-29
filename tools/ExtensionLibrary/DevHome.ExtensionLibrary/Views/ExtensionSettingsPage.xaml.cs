// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace DevHome.ExtensionLibrary.Views;

public sealed partial class ExtensionSettingsPage : DevHomePage
{
    public ExtensionSettingsViewModel ViewModel { get; }

    private readonly IThemeSelectorService _themeSelectorService;

    public ExtensionSettingsPage()
    {
        ViewModel = Application.Current.GetService<ExtensionSettingsViewModel>();
        _themeSelectorService = Application.Current.GetService<IThemeSelectorService>();
        this.InitializeComponent();
        ViewModel.SettingsContentLoaded += async () => await OnSettingsContentLoadedAsync();
        _themeSelectorService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, ElementTheme e)
    {
        UpdateTheme(webView2);
        webView2.Reload();
    }

    private async Task OnSettingsContentLoadedAsync()
    {
        if (ViewModel.IsWebView2Visible)
        {
            await InitializeWebViewAsync();
        }
    }

    private async Task InitializeWebViewAsync()
    {
        await webView2.EnsureCoreWebView2Async();
        webView2.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        UpdateTheme(webView2);
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string message = e.TryGetWebMessageAsString();
        if (message == "buttonClicked")
        {
            ViewModel.RenderAdaptiveCard();
        }
    }

    public void UpdateTheme(WebView2 webView2)
    {
        try
        {
            webView2.CoreWebView2.Profile.PreferredColorScheme = (_themeSelectorService.GetActualTheme() == ElementTheme.Dark) ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message + "\n");
        }
    }
}
