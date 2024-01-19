// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using CoreWidgetProvider.Helpers;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("F8B2DBB9-3687-4C6E-99B2-B92C82905937")]
internal class WidgetProvider : IWidgetProvider, IWidgetProvider2
{
    private readonly Dictionary<string, IWidgetImplFactory> widgetDefinitionRegistry = new ();
    private readonly Dictionary<string, WidgetImpl> runningWidgets = new ();

    public WidgetProvider()
    {
        Log.Logger()?.ReportDebug("Provider Constructed");
        widgetDefinitionRegistry.Add("SSH_Wallet", new WidgetImplFactory<SSHWalletWidget>());
        widgetDefinitionRegistry.Add("System_Memory", new WidgetImplFactory<SystemMemoryWidget>());
        widgetDefinitionRegistry.Add("System_NetworkUsage", new WidgetImplFactory<SystemNetworkUsageWidget>());
        widgetDefinitionRegistry.Add("System_GPUUsage", new WidgetImplFactory<SystemGPUUsageWidget>());
        widgetDefinitionRegistry.Add("System_CPUUsage", new WidgetImplFactory<SystemCPUUsageWidget>());
        RecoverRunningWidgets();
    }

    ~WidgetProvider()
    {
        Log.Logger()?.Dispose();
    }

    private void InitializeWidget(WidgetContext widgetContext, string state)
    {
        var widgetId = widgetContext.Id;
        var widgetDefinitionId = widgetContext.DefinitionId;
        Log.Logger()?.ReportDebug($"Calling Initialize for Widget Id: {widgetId} - {widgetDefinitionId}");

        if (!widgetDefinitionRegistry.ContainsKey(widgetDefinitionId))
        {
            Log.Logger()?.ReportError($"Unknown widget DefinitionId: {widgetDefinitionId}");
            return;
        }

        if (runningWidgets.ContainsKey(widgetId))
        {
            Log.Logger()?.ReportWarn($"Attempted to initialize a widget twice: {widgetDefinitionId} - {widgetId}");
            return;
        }

        var factory = widgetDefinitionRegistry[widgetDefinitionId];
        var widgetImpl = factory.Create(widgetContext, state);
        runningWidgets.Add(widgetId, widgetImpl);
    }

    private void RecoverRunningWidgets()
    {
        WidgetInfo[] recoveredWidgets;
        try
        {
            recoveredWidgets = WidgetManager.GetDefault().GetWidgetInfos();

            if (recoveredWidgets is null)
            {
                Log.Logger()?.ReportDebug("No running widgets to recover.");
                return;
            }
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError("Failed retrieving list of running widgets.", e);
            return;
        }

        foreach (var widgetInfo in recoveredWidgets)
        {
            if (!runningWidgets.ContainsKey(widgetInfo.WidgetContext.Id))
            {
                InitializeWidget(widgetInfo.WidgetContext, widgetInfo.CustomState);
            }
        }

        Log.Logger()?.ReportDebug("Finished recovering widgets.");
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        Log.Logger()?.ReportInfo($"CreateWidget id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");
        InitializeWidget(widgetContext, string.Empty);
    }

    public void Activate(WidgetContext widgetContext)
    {
        Log.Logger()?.ReportDebug($"Activate id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].Activate(widgetContext);
        }
        else
        {
            // Called to activate a widget that we don't know about, which is unexpected. Try to recover by creating it.
            Log.Logger()?.ReportWarn($"Found WidgetId that was not known: {widgetContext.Id}, attempting to recover by creating it.");
            CreateWidget(widgetContext);
            if (runningWidgets.ContainsKey(widgetId))
            {
                runningWidgets[widgetId].Activate(widgetContext);
            }
        }
    }

    public void Deactivate(string widgetId)
    {
        Log.Logger()?.ReportDebug($"Deactivate id: {widgetId}");
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].Deactivate(widgetId);
        }
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        Log.Logger()?.ReportInfo($"DeleteWidget id: {widgetId}");
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].DeleteWidget(widgetId, customState);
            runningWidgets.Remove(widgetId);
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        Log.Logger()?.ReportDebug($"OnActionInvoked id: {actionInvokedArgs.WidgetContext.Id} definitionId: {actionInvokedArgs.WidgetContext.DefinitionId}");
        var widgetContext = actionInvokedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].OnActionInvoked(actionInvokedArgs);
        }
    }

    public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        Log.Logger()?.ReportDebug($"OnCustomizationRequested id: {customizationRequestedArgs.WidgetContext.Id} definitionId: {customizationRequestedArgs.WidgetContext.DefinitionId}");
        var widgetContext = customizationRequestedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].OnCustomizationRequested(customizationRequestedArgs);
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Log.Logger()?.ReportDebug($"OnWidgetContextChanged id: {contextChangedArgs.WidgetContext.Id} definitionId: {contextChangedArgs.WidgetContext.DefinitionId}");
        var widgetContext = contextChangedArgs.WidgetContext;
        var widgetId = widgetContext.Id;
        if (runningWidgets.ContainsKey(widgetId))
        {
            runningWidgets[widgetId].OnWidgetContextChanged(contextChangedArgs);
        }
    }
}
