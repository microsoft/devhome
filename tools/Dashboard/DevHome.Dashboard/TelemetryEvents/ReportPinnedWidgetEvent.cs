// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Common.Helpers;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Dashboard.TelemetryEvents;

[EventData]
public class ReportPinnedWidgetEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public Guid DeploymentIdentifier { get; }

    public string WidgetProviderDefinitionId { get; }

    public string WidgetDefinitionId { get; }

    public ReportPinnedWidgetEvent(string widgetProviderDefinitionId, string widgetDefinitionId)
    {
        DeploymentIdentifier = Deployment.Identifier;
        WidgetProviderDefinitionId = widgetProviderDefinitionId;
        WidgetDefinitionId = widgetDefinitionId;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
