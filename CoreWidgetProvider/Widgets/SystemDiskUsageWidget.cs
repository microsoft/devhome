// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;
internal class SystemDiskUsageWidget : CoreWidget, IDisposable
{
    private readonly DataManager dataManager;

    public SystemDiskUsageWidget()
    : base()
    {
        dataManager = new (DataType.Disk, UpdateWidget);
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => @"Widgets\Templates\SystemDiskUsageTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\SystemDiskUsageTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting disk Data");

        try
        {
            var diskViewData = new JsonObject();

            var diskStats = dataManager.GetDiskStats();
            var diskName = diskStats.GetDiskName(0);
            var diskData = diskStats.GetDiskData(0);

            diskViewData.Add("diskUsagePercentage", diskData.Usage.ToString(CultureInfo.InvariantCulture));
            diskViewData.Add("diskRead", BytesToHumanString(diskData.ReadBytesPerSecond));
            diskViewData.Add("diskWrite", BytesToHumanString(diskData.WriteBytesPerSecond));
            diskViewData.Add("diskName", diskName);
            diskViewData.Add("diskGraphUrl", diskStats.CreateDiskImageUrl(0));

            DataState = WidgetDataState.Okay;
            ContentData = diskViewData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving disk data.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    private string BytesToHumanString(ulong bytes)
    {
        if (bytes < 1024)
        {
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        var memSize = bytes / 1024.0;
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

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => EmptyJson,
        };
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.PrevItem:
                break;

            case WidgetAction.NextItem:
                break;

            case WidgetAction.Unknown:
                Log.Logger()?.ReportError(Name, ShortId, $"Unknown verb: {actionInvokedArgs.Verb}");
                break;
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

    public void Dispose() => dataManager.Dispose();
}
