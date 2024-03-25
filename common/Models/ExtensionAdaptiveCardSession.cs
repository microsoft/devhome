// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Helpers;

using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Common.Models;

/// <summary>
/// Wrapper class for the IExtensionAdaptiveCardSession and IExtensionAdaptiveCardSession2 interfaces.
/// </summary>
public class ExtensionAdaptiveCardSession
{
    private readonly string _componentName = "ExtensionAdaptiveCardSession";

    private readonly IExtensionAdaptiveCardSession _cardSession;

    public event TypedEventHandler<ExtensionAdaptiveCardSession, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public ExtensionAdaptiveCardSession(IExtensionAdaptiveCardSession cardSession)
    {
        _cardSession = cardSession;

        if (_cardSession is IExtensionAdaptiveCardSession2 cardSession2)
        {
            cardSession2.Stopped += OnSessionStopped;
        }
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        try
        {
            return _cardSession.Initialize(extensionUI);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"Initialize failed due to exception", ex);
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
        }
    }

    public void Dispose()
    {
        try
        {
            _cardSession.Dispose();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"Dispose failed due to exception", ex);
        }

        if (_cardSession is IExtensionAdaptiveCardSession2 cardSession2)
        {
            cardSession2.Stopped -= OnSessionStopped;
        }
    }

    public async Task<ProviderOperationResult> OnAction(string action, string inputs)
    {
        try
        {
            return await _cardSession.OnAction(action, inputs);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"OnAction failed due to exception", ex);
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
        }
    }

    public void OnSessionStopped(IExtensionAdaptiveCardSession2 sender, ExtensionAdaptiveCardSessionStoppedEventArgs args)
    {
        Stopped?.Invoke(this, args);
    }
}
