// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;
using Windows.System;

namespace CoreWidgetProvider.Widgets;
internal class SystemUpdateWidget : CoreWidget, IDisposable
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(SystemUpdateWidget);

    public SystemUpdateWidget()
        : base()
    {
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Update Data");

        try
        {
            var updateData = new JsonObject();
            Log.Logger()?.ReportDebug("Update Info");
            var lastChecked = GetLastUpdateDate();
            var listOfUpdates = GetListOfUpdates();

            updateData.Add("lastChecked", lastChecked);
            updateData.Add("listOfUpdates", listOfUpdates);
            ContentData = updateData.ToJsonString();
            DataState = WidgetDataState.Okay;
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    // TODO : Need to get the last windows update checked date
    public string GetLastUpdateDate() => string.Empty;

    // TODO : Need to use system update agent api for get the list of updates
    public JsonArray GetListOfUpdates()
    {
        var listOfUpdates = new JsonArray();
        var hostJson = new JsonObject
         {
            { "title",  "XXXXXXXXXXXXXXX" },
            { "status", "Not Installed" },
         };

        ((IList<JsonNode?>)listOfUpdates).Add(hostJson);
        return listOfUpdates;
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => @"Widgets\Templates\SystemUpdateTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\SystemUpdateTemplate.json",
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

    public async override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.CheckForUpdates:
                await Launcher.LaunchUriAsync(new ("ms-settings:windowsupdate"));
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

        LogCurrentState();
        UpdateWidget();
    }

    protected override void SetInactive()
    {
        ActivityState = WidgetActivityState.Inactive;

        LogCurrentState();
    }

    protected override void SetDeleted()
    {
        SetState(string.Empty);
        ActivityState = WidgetActivityState.Unknown;
        LogCurrentState();
    }

    public void Dispose()
    {
    }
}
