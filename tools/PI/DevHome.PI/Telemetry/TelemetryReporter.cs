// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.PI;
using DevHome.PI.Models;
using DevHome.PI.SettingsUi;
using DevHome.PI.Telemetry;
using DevHome.PI.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Serilog;
using WinUIEx;
using static DevHome.PI.Telemetry.FeatureState;

namespace DevHome.PI.Telemetry;

internal sealed class TelemetryReporter : IDisposable
{
    private readonly FeatureState currentFeatureState = new();
    private WindowEventGenerator? eventGenerator;
    private static readonly Mutex FeatureMutex = new();
    private static readonly ILogger Log = Serilog.Log.ForContext("SourceContext", nameof(TelemetryReporter));

    private static Feature currentFeatureSet;

    internal static Feature CurrentFeatureSet => currentFeatureSet;

    public TelemetryReporter()
    {
    }

    internal static void SetWindow(Window win)
    {
        ArgumentNullException.ThrowIfNull(win, nameof(win));

        // Make sure to apply all this to the already existing service object.
        var rep = Application.Current.GetService<TelemetryReporter>();
        rep.eventGenerator = new WindowEventGenerator(win);
        rep.eventGenerator.InteractiveUsageVisibilityEvent += rep.InteractiveUsageVisibilityEvent;
        rep.eventGenerator.InteractiveUsageFocusEvent += rep.InteractiveUsageFocusEvent;
    }

    public void SwitchTo(Feature requestedFeature)
    {
        if (FeatureMutex.WaitOne())
        {
            if (FeatureState.IsExclusive(requestedFeature))
            {
                var foundFirst = false;

                // Use a copy of the flags, since we'll be modifying them inside the loop.
                var currentFeatureSetIterator = currentFeatureSet.GetFlags();
                foreach (var flag in currentFeatureSetIterator)
                {
                    // Skip empty flag.
                    if (flag == Feature.None)
                    {
                        continue;
                    }

                    if (foundFirst)
                    {
                        continue;
                    }

                    // If the requested feature is exclusive and the current iteration feature is exclusive,
                    // remove it as we're about to add an exclusive.
                    if (FeatureState.IsExclusive(flag))
                    {
                        // First fire any events to stop the previous features telemetry.
                        // Debug.Assert(eventGenerator != null, "eventGenerator is null");
                        if (eventGenerator != null)
                        {
                            var eventArgs = new WindowEventGenerator.InteractiveUsageEventArgs(WindowEventGenerator.InteractiveUsageEventType.Stop);
                            if (eventGenerator.CurrentlyVisible)
                            {
                                InteractiveUsageVisibilityEvent(this, eventArgs);
                            }

                            if (eventGenerator.CurrentlyFocused)
                            {
                                InteractiveUsageFocusEvent(this, eventArgs);
                            }
                        }

                        // Remove existing exclusive features.
                        currentFeatureSet &= ~flag;

                        Log.Debug("Removed an exclusive feature {0}.", flag);
                        Log.Debug("currentFeatureSet = {0}", currentFeatureSet.ToString());

                        // Leave a mark that we've encountered our first exclusive feature.  There should only ever be one.
                        foundFirst = true;
                    }
                }
            }

            // Add the feature to the set.
            currentFeatureSet |= requestedFeature;
            Log.Debug("Added the exclusive feature {0}.", requestedFeature);
            Log.Debug("currentFeatureSet = {0}", currentFeatureSet.ToString());
        }

        // TODO: Codepath probably should throw exception... (failed to acquire mutex...)
    }

    public Feature CurrentExclusive
    {
        get
        {
            foreach (var flag in currentFeatureSet.GetFlags())
            {
                // Skip empty flag.
                if (flag == Feature.None)
                {
                    continue;
                }

                // Return the first exclusive.
                if (FeatureState.IsExclusive(flag))
                {
                    return currentFeatureSet & flag;
                }
            }

            return Feature.None;
        }
    }

    private void InteractiveUsageVisibilityEvent(object? sender, WindowEventGenerator.InteractiveUsageEventArgs e)
    {
        var app = App.Current as DevHome.PI.App;
        if (app != null)
        {
            var featureName = CurrentExclusive.ToString();

            if (e.UsageType == WindowEventGenerator.InteractiveUsageEventType.Start)
            {
                App.Log<VisibilityStartEventData>("DevHome.PI_VisibilityStart", LogLevel.Measure, new VisibilityStartEventData(featureName, DateTime.Now));
                Log.Debug("Visibility Start!");
            }
            else
            {
                App.Log<VisibilityStopEventData>("DevHome.PI_VisibilityStop", LogLevel.Measure, new VisibilityStopEventData(featureName, DateTime.Now));
                Log.Debug("Visibility Stop");
            }
        }
    }

    private void InteractiveUsageFocusEvent(object? sender, WindowEventGenerator.InteractiveUsageEventArgs e)
    {
        var app = App.Current as DevHome.PI.App;
        if (app != null)
        {
            var featureName = CurrentExclusive.ToString();

            if (e.UsageType == WindowEventGenerator.InteractiveUsageEventType.Start)
            {
                App.Log<VisibilityStartEventData>("DevHome.PI_FocusStart", LogLevel.Measure, new VisibilityStartEventData(featureName, DateTime.Now));
                Log.Debug("Focus Start!");
            }
            else
            {
                App.Log<VisibilityStopEventData>("DevHome.PI_FocusStop", LogLevel.Measure, new VisibilityStopEventData(featureName, DateTime.Now));
                Log.Debug("Focus Stop");
            }
        }
    }

    public void Dispose()
    {
        eventGenerator?.Dispose();
    }
}
