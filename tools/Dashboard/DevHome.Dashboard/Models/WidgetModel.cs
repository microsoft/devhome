// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Models;

public partial class WidgetModel : ObservableObject
{
    [ObservableProperty]
    private Widget _widget;

    public string Id => Widget.Id;

    public string DefinitionId => Widget.DefinitionId;

    public WidgetModel(Widget widget)
    {
        Widget = widget;
    }

    public async Task<string> GetCardTemplateAsync()
    {
        return await Widget.GetCardTemplateAsync();
    }

    public async Task<string> GetCardDataAsync()
    {
        return await Widget.GetCardDataAsync();
    }

    public async Task NotifyActionInvokedAsync(string verb, string dataToSend)
    {
        await Widget.NotifyActionInvokedAsync(verb, dataToSend);
    }

    public async Task DeleteAsync()
    {
        await Widget.DeleteAsync();
    }

    public async Task<WidgetSize> GetSizeAsync()
    {
        return await Widget.GetSizeAsync();
    }

    public async Task<string> GetCustomStateAsync()
    {
        return await Widget.GetCustomStateAsync();
    }

    public async Task SetCustomStateAsync(string state)
    {
        await Widget.SetCustomStateAsync(state);
    }

    public async Task SetSizeAsync(WidgetSize size)
    {
        await Widget.SetSizeAsync(size);
    }

    public async Task NotifyCustomizationRequestedAsync()
    {
        await Widget.NotifyCustomizationRequestedAsync();
    }
}
