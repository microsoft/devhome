// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using Serilog;
using Windows.ApplicationModel.Preview.Notes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SampleExtension.Providers;

public class SettingsProvider2 : ISettingsProvider2
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(SettingsProvider2)));

    private static readonly ILogger _log = _logger.Value;
    private readonly WebViewResult _webViewResult;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public DisplayType DisplayType => DisplayType.WebView2;

    public SettingsProvider2()
    {
        _webViewResult = new WebViewResult(new NotImplementedException(), "No WebView was provided for the SettingsProvider2");
    }

    public SettingsProvider2(WebViewResult webViewResult)
    {
        _webViewResult = webViewResult;
    }

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");

        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public WebViewResult GetWebView()
    {
        if (_webViewResult.Url == string.Empty)
        {
            string emptyUrlErrorMessage = "Error in SettingsProvider2.GetWebView(): WebViewResult.Url is empty";
            _log.Error(emptyUrlErrorMessage);
            throw new NotImplementedException(emptyUrlErrorMessage);
        }

        _log.Information($"GetWebView. URL: {_webViewResult.Url}");

        return _webViewResult;
    }
}
