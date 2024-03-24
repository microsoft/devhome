// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class AdaptiveSettingsCard : IAdaptiveCardElement
{
    // Start of IAdaptiveCardElement properties
    public JsonObject AdditionalProperties { get; set; } = new();

    public ElementType ElementType { get; set; } = ElementType.Custom;

    public string ElementTypeString { get; set; } = AdaptiveSettingsCardType;

    public static string AdaptiveSettingsCardType => "DevHome.AdaptiveSettingsCard";

    public IAdaptiveCardElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; }

    public HeightType Height { get; set; } = HeightType.Stretch;

    public string Id { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public IList<AdaptiveRequirement> Requirements { get; set; } = new List<AdaptiveRequirement>();

    public bool Separator { get; set; }

    public Spacing Spacing { get; set; } = Spacing.Default;

    // Unused at this time
    public JsonObject ToJson() => new();

    // Start of AdaptiveSettingsCard properties.
    // These properties relate to the Windows Community Toolkit's SettingsCard control.
    // We'll allow extensions to provide the data for the SettingsCard control from an Adaptive Card.
    // Then we'll render the actual SettingsCard control in the DevHome app.
    public string Description { get; set; } = string.Empty;

    public string SubDescription { get; set; } = string.Empty;

    public string IconElement { get; set; }

    public string InnerAdaptiveCardJsonTemplate { get; set; }

    public string InnerAdaptiveCardJsonData { get; set; }

    public string InnerAdaptiveCardTitle { get; set; } = string.Empty;

    public string ActionButtonText { get; set; } = string.Empty;

    public bool ShouldShowActionItem { get; set; } = true;
}