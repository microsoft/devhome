// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.ExtensionLibrary.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage;
using Windows.UI.WebUI;

namespace DevHome.ExtensionLibrary.Views;

public sealed partial class ExtensionSettingsPage : DevHomePage
{
    public ExtensionSettingsViewModel ViewModel { get; }

    public ExtensionSettingsPage()
    {
        ViewModel = Application.Current.GetService<ExtensionSettingsViewModel>();
        this.InitializeComponent();
    }

    private async void InitializeWebView2Async()
    {
        await webView2.EnsureCoreWebView2Async();

        Console.WriteLine("Theme : " + this.ActualTheme);

        webView2.CoreWebView2.Profile.PreferredColorScheme = (this.ActualTheme == ElementTheme.Dark) ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
    }
}
