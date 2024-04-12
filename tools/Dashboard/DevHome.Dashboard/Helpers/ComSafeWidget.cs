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

namespace DevHome.Dashboard.Helpers;

/// <summary>
/// Since Widgets are OOP COM objects, we need to wrap them in a safe way to handle COM exceptions
/// that arise when the underlying OOP object vanishes. All Widgets should be wrapped in a
/// ComSafeWidget and calls to the widget should be done through the ComSafeWidget.
/// This class will handle the COM exceptions and get a new OOP Widget if needed.
/// All APIs on the IWidget and IWidget2 interfaces are reflected here.
/// </summary>
public class ComSafeWidget
{
    public Widget OopWidget { get; set; }

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
        OopWidget = widget;
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
                return await OopWidget.GetCardTemplateAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                return await OopWidget.GetCardTemplateAsync();
            }
        });
    }

    public async Task<string> GetCardDataAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                return await OopWidget.GetCardDataAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                return await OopWidget.GetCardDataAsync();
            }
        });
    }

    public async Task<string> GetCustomStateAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                return await OopWidget.GetCustomStateAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                return await OopWidget.GetCustomStateAsync();
            }
        });
    }

    public async Task<WidgetSize> GetSizeAsync()
    {
        return await Task.Run(async () =>
        {
            try
            {
                return await OopWidget.GetSizeAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                return await OopWidget.GetSizeAsync();
            }
        });
    }

    public async Task NotifyActionInvokedAsync(string verb, string data)
    {
        await Task.Run(async () =>
        {
            try
            {
                await OopWidget.NotifyActionInvokedAsync(verb, data);
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                await OopWidget.NotifyActionInvokedAsync(verb, data);
            }
        });
    }

    public async Task DeleteAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                await OopWidget.DeleteAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                await OopWidget.DeleteAsync();
            }
        });
    }

    public async Task SetCustomStateAsync(string state)
    {
        await Task.Run(async () =>
        {
            try
            {
                await OopWidget.SetCustomStateAsync(state);
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                await OopWidget.SetCustomStateAsync(state);
            }
        });
    }

    public async Task SetSizeAsync(WidgetSize widgetSize)
    {
        await Task.Run(async () =>
        {
            try
            {
                await OopWidget.SetSizeAsync(widgetSize);
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                await OopWidget.SetSizeAsync(widgetSize);
            }
        });
    }

    public async Task NotifyCustomizationRequestedAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                await OopWidget.NotifyCustomizationRequestedAsync();
            }
            catch (COMException e) when (e.HResult == RpcServerUnavailable || e.HResult == RpcCallFailed)
            {
                await GetNewOopWidgetAsync();
                await OopWidget.NotifyCustomizationRequestedAsync();
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
        OopWidget = host.GetWidget(Id);

        DefinitionId = OopWidget.DefinitionId;
        Id = OopWidget.Id;
    }
}
