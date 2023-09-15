// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

internal class SystemCPUUsageWidget : CoreWidget, IDisposable
{
    private static Dictionary<string, string> Templates { get; set; } = new();

    protected static readonly new string Name = nameof(SystemCPUUsageWidget);

    private readonly DataManager dataManager;

    public SystemCPUUsageWidget()
        : base()
    {
        dataManager = new(DataType.CPU, UpdateWidget);
    }

    private string SpeedToString(float cpuSpeed)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:0.00} GHz", cpuSpeed / 1000);
    }

    private string FloatToPercentString(float value)
    {
        return ((int)(value * 100)).ToString(CultureInfo.InvariantCulture) + "%";
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting CPU stats");

        try
        {
            var cpuData = new JsonObject();

            var currentData = dataManager.GetCPUStats();

            cpuData.Add("cpuUsage", FloatToPercentString(currentData.CpuUsage));
            cpuData.Add("cpuSpeed", SpeedToString(currentData.CpuSpeed));
            cpuData.Add("cpuGraphUrl", currentData.CreateCPUImageUrl());
            cpuData.Add("chartHeight", ChartHelper.ChartHeight + "px");
            cpuData.Add("chartWidth", ChartHelper.ChartWidth + "px");

            cpuData.Add("cpuProc1", currentData.GetCpuProcessText(0));
            cpuData.Add("cpuProc2", currentData.GetCpuProcessText(1));
            cpuData.Add("cpuProc3", currentData.GetCpuProcessText(2));

            DataState = WidgetDataState.Okay;
            ContentData = cpuData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving stats.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => @"Widgets\Templates\SystemCPUUsageTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\SystemCPUUsageTemplate.json",
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

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        var processIndex = -1;
        switch (verb)
        {
            case WidgetAction.CpuKill1:
                processIndex = 0;
                break;

            case WidgetAction.CpuKill2:
                processIndex = 1;
                break;

            case WidgetAction.CpuKill3:
                processIndex = 2;
                break;

            case WidgetAction.Unknown:
                Log.Logger()?.ReportError(Name, ShortId, $"Unknown verb: {actionInvokedArgs.Verb}");
                break;
        }

        if (processIndex != -1)
        {
            dataManager.GetCPUStats().KillTopProcess(processIndex);
        }
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
