// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text;
using System.Text.Json.Nodes;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

internal abstract class CoreWidget : WidgetImpl
{
    protected static readonly string EmptyJson = new JsonObject().ToJsonString();

    protected static readonly string Name = nameof(CoreWidget);

    protected WidgetActivityState ActivityState { get; set; } = WidgetActivityState.Unknown;

    protected WidgetDataState DataState { get; set; } = WidgetDataState.Unknown;

    protected WidgetPageState Page { get; set; } = WidgetPageState.Unknown;

    protected string ContentData { get; set; } = EmptyJson;

    protected bool Enabled
    {
        get; set;
    }

    protected Dictionary<WidgetPageState, string> Template { get; set; } = new ();

    public CoreWidget()
    {
    }

    public virtual string GetConfiguration(string data) => throw new NotImplementedException();

    public virtual void LoadContentData() => throw new NotImplementedException();

    public override void CreateWidget(WidgetContext widgetContext, string state)
    {
        Id = widgetContext.Id;
        Enabled = widgetContext.IsActive;
        UpdateActivityState();
    }

    public override void Activate(WidgetContext widgetContext)
    {
        Enabled = true;
        UpdateActivityState();
    }

    public override void Deactivate(string widgetId)
    {
        Enabled = false;
        UpdateActivityState();
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        Enabled = false;
        SetDeleted();
    }

    public override void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Enabled = contextChangedArgs.WidgetContext.IsActive;
        UpdateActivityState();
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs) => throw new NotImplementedException();

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs) => throw new NotImplementedException();

    protected WidgetAction GetWidgetActionForVerb(string verb)
    {
        try
        {
            return Enum.Parse<WidgetAction>(verb);
        }
        catch (Exception)
        {
            // Invalid verb.
            Log.Logger()?.ReportError($"Unknown WidgetAction verb: {verb}");
            return WidgetAction.Unknown;
        }
    }

    public virtual void UpdateActivityState()
    {
        if (Enabled)
        {
            SetActive();
            return;
        }

        SetInactive();
    }

    public virtual void UpdateWidget()
    {
        LoadContentData();

        WidgetUpdateRequestOptions updateOptions = new (Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
            CustomState = State(),
        };

        Log.Logger()?.ReportDebug(Name, ShortId, $"Updating widget for {Page}");
        WidgetManager.GetDefault().UpdateWidget(updateOptions);
    }

    public virtual string GetTemplatePath(WidgetPageState page)
    {
        return string.Empty;
    }

    public virtual string GetData(WidgetPageState page)
    {
        return string.Empty;
    }

    protected string GetTemplateForPage(WidgetPageState page)
    {
        if (Template.ContainsKey(page))
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Using cached template for {page}");
            return Template[page];
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, GetTemplatePath(page));
            var template = File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
            template = Resources.ReplaceIdentifers(template, Resources.GetWidgetResourceIdentifiers(), Log.Logger());
            Log.Logger()?.ReportDebug(Name, ShortId, $"Caching template for {page}");
            Template[page] = template;
            return template;
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error getting template.", e);
            return string.Empty;
        }
    }

    protected string GetCurrentState()
    {
        return $"State: {ActivityState}  Page: {Page}  Data: {DataState}  State: {State()}";
    }

    protected void LogCurrentState()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, GetCurrentState());
    }

    protected virtual void SetActive()
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

    protected virtual void SetInactive()
    {
        ActivityState = WidgetActivityState.Inactive;

        LogCurrentState();
    }

    protected virtual void SetDeleted()
    {
        SetState(string.Empty);
        ActivityState = WidgetActivityState.Unknown;
        LogCurrentState();
    }
}
