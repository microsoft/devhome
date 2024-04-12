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
    private Widget _oopWidget;

    // Not currently used.
    public DateTimeOffset DataLastUpdated => throw new NotImplementedException();

    public string DefinitionId { get; private set; }

    public string Id { get; private set; }

    // Not currently used.
    public DateTimeOffset TemplateLastUpdated => throw new NotImplementedException();

    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    public ComSafeWidget(Widget widget)
    {
        _oopWidget = widget;

        DefinitionId = widget.DefinitionId;
        Id = widget.Id;
        widget.WidgetUpdated += OopWidgetUpdated;
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
                return await _oopWidget.GetCardTemplateAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                return await _oopWidget.GetCardDataAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                return await _oopWidget.GetCustomStateAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                return await _oopWidget.GetSizeAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                await _oopWidget.NotifyActionInvokedAsync(verb, data);
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                await _oopWidget.DeleteAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                await _oopWidget.SetCustomStateAsync(state);
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                await _oopWidget.SetSizeAsync(widgetSize);
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
                await _oopWidget.NotifyCustomizationRequestedAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
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
        var hostingService = Application.Current.GetService<IWidgetHostingService>();
        var host = await hostingService.GetWidgetHostAsync();
        _oopWidget = host.GetWidget(Id);

        DefinitionId = _oopWidget.DefinitionId;
        Id = _oopWidget.Id;
    }
}
