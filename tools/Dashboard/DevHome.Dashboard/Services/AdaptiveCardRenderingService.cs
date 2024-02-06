// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Renderers;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Helpers;
using Microsoft.UI.Xaml;
using Windows.Storage;
using WinUIEx;

namespace DevHome.Dashboard.Services;

public class AdaptiveCardRenderingService : IAdaptiveCardRenderingService, IDisposable
{
    private readonly WindowEx _windowEx;

    private readonly IThemeSelectorService _themeSelectorService;

    private readonly SemaphoreSlim _rendererLock = new(1, 1);

    private AdaptiveCardRenderer _renderer;

    private bool _disposedValue;

    public AdaptiveCardRenderingService(WindowEx windowEx, IThemeSelectorService themeSelectorService)
    {
        _windowEx = windowEx;
        _themeSelectorService = themeSelectorService;
        _themeSelectorService.ThemeChanged += OnThemeChanged;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _rendererLock.Dispose();
            }

            _disposedValue = true;
        }
    }

    public async Task<AdaptiveCardRenderer> GetRenderer()
    {
        // We need to lock the renderer, otherwise another widget could come in after the renderer
        // is created but before it is configured and render the widget without configuration.
        await _rendererLock.WaitAsync();
        try
        {
            if (_renderer == null)
            {
                _renderer = new AdaptiveCardRenderer();
                await ConfigureWidgetRenderer();
            }

            return _renderer;
        }
        finally
        {
            _rendererLock.Release();
        }
    }

    private async Task ConfigureWidgetRenderer()
    {
        // Add custom Adaptive Card renderer.
        _renderer.ElementRenderers.Set(LabelGroup.CustomTypeString, new LabelGroupRenderer());

        // A different host config is used to render widgets (adaptive cards) in light and dark themes.
        await UpdateHostConfig();
    }

    public async Task UpdateHostConfig()
    {
        if (_renderer != null)
        {
            // Add host config for current theme.
            var hostConfigContents = string.Empty;
            var hostConfigFileName = _themeSelectorService.IsDarkTheme() ? "HostConfigDark.json" : "HostConfigLight.json";
            try
            {
                Log.Logger()?.ReportInfo("DashboardView", $"Get HostConfig file '{hostConfigFileName}'");
                var uri = new Uri($"ms-appx:///DevHome.Dashboard/Assets/{hostConfigFileName}");
                var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
                hostConfigContents = await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError("DashboardView", "Error retrieving HostConfig", ex);
            }

            _windowEx.DispatcherQueue.TryEnqueue(() =>
            {
                if (!string.IsNullOrEmpty(hostConfigContents))
                {
                    _renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
                }
                else
                {
                    Log.Logger()?.ReportError("DashboardView", $"HostConfig contents are {hostConfigContents}");
                }
            });
        }
    }

    private async void OnThemeChanged(object sender, ElementTheme e)
    {
        await UpdateHostConfig();
    }
}
