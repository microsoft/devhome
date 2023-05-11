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

    private string MemUlongToString(ulong memBytes)
    {
        if (memBytes < 1024)
        {
            return memBytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        var memSize = memBytes / 1024.0;
        if (memSize < 1024)
        {
            return memSize.ToString("0.00", CultureInfo.InvariantCulture) + " kB";
        }

        memSize /= 1024;
        if (memSize < 1024)
        {
            return memSize.ToString("0.00", CultureInfo.InvariantCulture) + " MB";
        }

        memSize /= 1024;
        return memSize.ToString("0.00", CultureInfo.InvariantCulture) + " GB";
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Memory usage data");

        try
        {
            var memoryData = new JsonObject();

            var currentData = dataManager.GetMemoryStats();

            memoryData.Add("allMem", MemUlongToString(currentData.AllMem));
            memoryData.Add("usedMem", MemUlongToString(currentData.UsedMem));
            memoryData.Add("memUsage", FloatToPercentString(currentData.MemUsage));
            memoryData.Add("commitedMem", MemUlongToString(currentData.MemCommited));
            memoryData.Add("commitedLimitMem", MemUlongToString(currentData.MemCommitLimit));
            memoryData.Add("cachedMem", MemUlongToString(currentData.MemCached));
            memoryData.Add("pagedPoolMem", MemUlongToString(currentData.MemPagedPool));
            memoryData.Add("nonPagedPoolMem", MemUlongToString(currentData.MemNonPagedPool));
            memoryData.Add("memGraphUrl", currentData.CreateMemImageUrl());

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
