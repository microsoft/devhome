// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;

namespace CoreWidgetProvider.Widgets;

internal class SystemMemoryWidget : CoreWidget, IDisposable
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(SystemMemoryWidget);

    private readonly DataManager dataManager;

    public SystemMemoryWidget()
        : base()
    {
        dataManager = new DataManager(UpdateWidget);
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        // Remove event handler
        base.DeleteWidget(widgetId, customState);
    }

    private string FloatToPercentString(float value)
    {
        return ((int)(value * 100)).ToString(CultureInfo.InvariantCulture) + "%";
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Pull Requests");

        try
        {
            var memoryData = new JsonObject();

            var currentData = dataManager.GetMemoryStats();

            memoryData.Add("allMem", currentData.AllMem);
            memoryData.Add("usedMem", currentData.UsedMem);
            memoryData.Add("memUsage", FloatToPercentString(currentData.MemUsage));
            memoryData.Add("commitedMem", currentData.MemCommited);
            memoryData.Add("commitedLimitMem", currentData.MemCommitLimit);
            memoryData.Add("cachedMem", currentData.MemCached);
            memoryData.Add("pagedPoolMem", currentData.MemPagedPool);
            memoryData.Add("nonPagedPoolMem", currentData.MemNonPagedPool);
            memoryData.Add("memGraphUrl", dataManager.CreateMemImageUrl());

            DataState = WidgetDataState.Okay;
            ContentData = memoryData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => @"Widgets\Templates\SystemMemoryTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\SystemMemoryTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        LoadContentData();

        return page switch
        {
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,

            // In case of unknown state default to empty data
            _ => EmptyJson,
        };
    }

    public void Dispose()
    {
        dataManager.Dispose();
    }
}
