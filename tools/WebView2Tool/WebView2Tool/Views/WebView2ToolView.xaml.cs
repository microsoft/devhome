// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Views;
using Microsoft.UI.Xaml.Input;

namespace DevHome.WebView2Tool.Views;

public partial class WebView2ToolView : ToolPage
{
    public WebView2ToolView()
    {
        this.InitializeComponent();

        addressBar.Text = "https://developer.microsoft.com/en-us/microsoft-edge/webview2/";
        webView2.Source = new Uri(addressBar.Text);

        InitializeWebView2Async();
    }

    private async void InitializeWebView2Async()
    {
        await webView2.EnsureCoreWebView2Async();
    }

    private void AddressBar_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            TryNavigate();
        }
    }

    private void TryNavigate()
    {
        Uri destinationUri;
        if (TryCreateUri(addressBar.Text, out destinationUri))
        {
            webView2.Source = destinationUri;
        }
        else
        {
            var bingString = "https://www.bing.com/search?q=" + Uri.EscapeDataString(addressBar.Text);
            if (TryCreateUri(bingString, out destinationUri))
            {
                addressBar.Text = destinationUri.AbsoluteUri;
                webView2.Source = destinationUri;
            }
            else
            {
            }
        }
    }

    private bool TryCreateUri(string potentialUri, out Uri result)
    {
        Uri uri;
        if ((Uri.TryCreate(potentialUri, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + potentialUri, UriKind.Absolute, out uri)) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            result = uri;
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }
}
