// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public abstract class DevHomeAdaptiveCardActionBase : IAdaptiveActionElement
{
    public ActionType ActionType { get; set; } = ActionType.Custom;

    public string ActionTypeString { get; set; } = string.Empty;

    public JsonObject? AdditionalProperties { get; set; }

    public IAdaptiveActionElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; }

    public string IconUrl { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public ActionMode Mode { get; set; }

    public ActionRole Role { get; set; }

    public string Style { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Tooltip { get; set; } = string.Empty;

    public JsonObject ToJson() => [];
}
