// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

internal sealed class SystemNetworkUsageWidget : CoreWidget, IDisposable
{
    private readonly DataManager dataManager;

    private static Dictionary<string, string> Templates { get; set; } = new();

    private int networkIndex;

    public SystemNetworkUsageWidget()
        : base()
    {
        dataManager = new(DataType.Network, UpdateWidget);
    }

    private string SpeedToString(float cpuSpeed)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:0.00} GHz", cpuSpeed / 1000);
    }

    private string FloatToPercentString(float value)
    {
        return ((int)(value * 100)).ToString(CultureInfo.InvariantCulture) + "%";
    }

    private string BytesToBitsPerSecString(float value)
    {
        // Bytes to bits
        value *= 8;

        // bits to Kbits
        value /= 1024;
        if (value < 1024)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} Kbps", value);
        }

        // Kbits to Mbits
        value /= 1024;
        return string.Format(CultureInfo.InvariantCulture, "{0:0.0} Mbps", value);
    }

    public override void LoadContentData()
    {
        Log.Debug("Getting network Data");

        try
        {
            var networkData = new JsonObject();

            var currentData = dataManager.GetNetworkStats();

            var netName = currentData.GetNetworkName(networkIndex);
            var networkStats = currentData.GetNetworkUsage(networkIndex);

            networkData.Add("networkUsage", FloatToPercentString(networkStats.Usage));
            networkData.Add("netSent", BytesToBitsPerSecString(networkStats.Sent));
            networkData.Add("netReceived", BytesToBitsPerSecString(networkStats.Received));
            networkData.Add("networkName", netName);
            networkData.Add("netGraphUrl", currentData.CreateNetImageUrl(networkIndex));
            networkData.Add("chartHeight", ChartHelper.ChartHeight + "px");
            networkData.Add("chartWidth", ChartHelper.ChartWidth + "px");

            DataState = WidgetDataState.Okay;
            ContentData = networkData.ToJsonString();
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
            WidgetPageState.Content => @"Widgets\Templates\SystemNetworkUsageTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\SystemNetworkUsageTemplate.json",
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

    private void HandlePrevNetwork(WidgetActionInvokedArgs args)
    {
        networkIndex = dataManager.GetNetworkStats().GetPrevNetworkIndex(networkIndex);
        UpdateWidget();
    }

    private void HandleNextNetwork(WidgetActionInvokedArgs args)
    {
        networkIndex = dataManager.GetNetworkStats().GetNextNetworkIndex(networkIndex);
        UpdateWidget();
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Debug($"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.PrevItem:
                HandlePrevNetwork(actionInvokedArgs);
                break;

            case WidgetAction.NextItem:
                HandleNextNetwork(actionInvokedArgs);
                break;

            case WidgetAction.Unknown:
                Log.Error($"Unknown verb: {actionInvokedArgs.Verb}");
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

    public void Dispose()
    {
        dataManager.Dispose();
    }
}
