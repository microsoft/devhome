// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;

namespace CoreWidgetProvider.Widgets;

internal sealed class SystemMemoryWidget : CoreWidget, IDisposable
{
    private static Dictionary<string, string> Templates { get; set; } = new();

    private readonly DataManager dataManager;

    public SystemMemoryWidget()
        : base()
    {
        dataManager = new(DataType.Memory, UpdateWidget);
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
        Log.Debug("Getting Memory usage data");

        try
        {
            var memoryData = new JsonObject();

            var currentData = dataManager.GetMemoryStats();

            memoryData.Add("allMem", MemUlongToString(currentData.AllMem));
            memoryData.Add("usedMem", MemUlongToString(currentData.UsedMem));
            memoryData.Add("memUsage", FloatToPercentString(currentData.MemUsage));
            memoryData.Add("committedMem", MemUlongToString(currentData.MemCommitted));
            memoryData.Add("committedLimitMem", MemUlongToString(currentData.MemCommitLimit));
            memoryData.Add("cachedMem", MemUlongToString(currentData.MemCached));
            memoryData.Add("pagedPoolMem", MemUlongToString(currentData.MemPagedPool));
            memoryData.Add("nonPagedPoolMem", MemUlongToString(currentData.MemNonPagedPool));
            memoryData.Add("memGraphUrl", currentData.CreateMemImageUrl());
            memoryData.Add("chartHeight", ChartHelper.ChartHeight + "px");
            memoryData.Add("chartWidth", ChartHelper.ChartWidth + "px");

            DataState = WidgetDataState.Okay;
            ContentData = memoryData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving data.");
            var content = new JsonObject
            {
                { "errorMessage", e.Message },
            };
            ContentData = content.ToJsonString();
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
        return page switch
        {
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,

            // In case of unknown state default to empty data
            _ => EmptyJson,
        };
    }

    protected override void SetActive()
    {
        ActivityState = WidgetActivityState.Active;
        Page = WidgetPageState.Content;
        if (ContentData == EmptyJson)
        {
            LoadContentData();
        }

        dataManager.Start();

        LogCurrentState();
        UpdateWidget();
    }

    protected override void SetInactive()
    {
        dataManager.Stop();

        ActivityState = WidgetActivityState.Inactive;

        LogCurrentState();
    }

    protected override void SetDeleted()
    {
        dataManager.Stop();

        SetState(string.Empty);
        ActivityState = WidgetActivityState.Unknown;
        LogCurrentState();
    }

    public void Dispose()
    {
        dataManager.Dispose();
    }
}
