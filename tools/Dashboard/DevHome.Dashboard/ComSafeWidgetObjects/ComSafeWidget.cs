// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Foundation;

namespace DevHome.Dashboard.ComSafeWidgetObjects;

/// <summary>
/// Since Widgets are OOP COM objects, we need to wrap them in a safe way to handle COM exceptions
/// that arise when the underlying OOP object vanishes. All Widgets should be wrapped in a
/// ComSafeWidget and calls to the widget should be done through the ComSafeWidget.
/// This class will handle the COM exceptions and get a new OOP Widget if needed.
/// All APIs on the IWidget and IWidget2 interfaces are reflected here.
/// </summary>
public class ComSafeWidget
{
    // Not currently used.
    public DateTimeOffset DataLastUpdated => throw new NotImplementedException();

    public string DefinitionId { get; private set; }

    public string Id { get; private set; }

    // Not currently used.
    public DateTimeOffset TemplateLastUpdated => throw new NotImplementedException();

    private Widget _oopWidget;

    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComSafeWidget));

    private bool _hasValidProperties;

    public ComSafeWidget(string widgetId)
    {
        Id = widgetId;
    }

    public event TypedEventHandler<ComSafeWidget, WidgetUpdatedEventArgs> WidgetUpdated = (_, _) => { };

    private void OopWidgetUpdated(Widget sender, WidgetUpdatedEventArgs args)
    {
        WidgetUpdated.Invoke(this, args);
    }

    public async Task<string> GetCardTemplateAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                return await _oopWidget.GetCardTemplateAsync();
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                return await _oopWidget.GetCardTemplateAsync();
            }
        });
    }

    public async Task<string> GetCardDataAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                return await _oopWidget.GetCardDataAsync();
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                return await _oopWidget.GetCardDataAsync();
            }
        });
    }

    public async Task<string> GetCustomStateAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                return await _oopWidget.GetCustomStateAsync();
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                return await _oopWidget.GetCustomStateAsync();
            }
        });
    }

    public async Task<WidgetSize> GetSizeAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                return await _oopWidget.GetSizeAsync();
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                return await _oopWidget.GetSizeAsync();
            }
        });
    }

    public async Task NotifyActionInvokedAsync(string verb, string data)
    {
        await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                await _oopWidget.NotifyActionInvokedAsync(verb, data);
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                await _oopWidget.NotifyActionInvokedAsync(verb, data);
            }
        });
    }

    public async Task DeleteAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                await _oopWidget.DeleteAsync();
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                await _oopWidget.DeleteAsync();
            }
        });
    }

    public async Task SetCustomStateAsync(string state)
    {
        await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                await _oopWidget.SetCustomStateAsync(state);
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                await _oopWidget.SetCustomStateAsync(state);
            }
        });
    }

    public async Task SetSizeAsync(WidgetSize widgetSize)
    {
        await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                await _oopWidget.SetSizeAsync(widgetSize);
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                await _oopWidget.SetSizeAsync(widgetSize);
            }
        });
    }

    public async Task NotifyCustomizationRequestedAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                await LazilyLoadOopWidget();
                await _oopWidget.NotifyCustomizationRequestedAsync();
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");
                await GetNewOopWidgetAsync();
                await _oopWidget.NotifyCustomizationRequestedAsync();
            }
        });
    }

    // Not currently used.
    public IAsyncAction NotifyAnalyticsInfoAsync(string analyticsInfo) => throw new NotImplementedException();

    // Not currently used.
    public IAsyncAction NotifyErrorInfoAsync(string errorInfo) => throw new NotImplementedException();

    private async Task GetNewOopWidgetAsync()
    {
        _oopWidget = null;
        _hasValidProperties = false;
        await LazilyLoadOopWidget();
    }

    private async Task LazilyLoadOopWidget()
    {
        var attempt = 0;
        while (attempt++ < 3)
        {
            try
            {
                _oopWidget ??= await Application.Current.GetService<IWidgetHostingService>().GetWidgetAsync(Id);

                if (!_hasValidProperties)
                {
                    await Task.Run(() =>
                    {
                        DefinitionId = _oopWidget.DefinitionId;
                        Id = _oopWidget.Id;
                        _oopWidget.WidgetUpdated += OopWidgetUpdated;
                        _hasValidProperties = true;
                    });
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to get properties of out-of-proc object");
            }
        }
    }

    /// <summary>
    /// Get a widget's ID from a widget object. Tries multiple times in case of COM exceptions.
    /// </summary>
    /// <param name="widget">Widget</param>
    /// <returns>The Widget's Id, or in the case of failure string.Empty</returns>
    public static async Task<string> GetIdFromUnsafeWidgetAsync(Widget widget)
    {
        var retries = 5;

        return await Task.Run(() =>
        {
            while (retries-- > 0)
            {
                try
                {
                    return widget.Id;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}, try {retries} more times");
                }
            }

            return string.Empty;
        });
    }
}
