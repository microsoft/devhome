// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class DevHomeAdaptiveSettingsCardItemsViewChoiceSet : IAdaptiveCardElement, IAdaptiveInputElement
{
    public ElementType ElementType { get; set; } = ElementType.Custom;

    // Specific properties for IAdaptiveCardElement
    public string ElementTypeString { get; set; } = AdaptiveSettingsCardType;

    public static string AdaptiveSettingsCardType => "DevHome.DevHomeAdaptiveSettingsCardItemsViewChoiceSet";

    // Specific properties for IAdaptiveInputElement
    public string ErrorMessage { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public string Label { get; set; } = string.Empty;

    // Specific properties for DevHomeAdaptiveSettingsCardItemsViewChoiceSet
    public IList<DevHomeAdaptiveSettingsCard> SettingsCards { get; set; } = [];

    public bool IsMultiSelect { get; set; }

    public string GroupDescription { get; set; } = string.Empty;

    public int SelectedValue { get; set; } = -1;

    public JsonObject AdditionalProperties { get; set; } = new();

    public IAdaptiveCardElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; }

    public HeightType Height { get; set; } = HeightType.Stretch;

    public string Id { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    public IList<AdaptiveRequirement> Requirements { get; set; } = new List<AdaptiveRequirement>();

    public bool Separator { get; set; }

    public Spacing Spacing { get; set; } = Spacing.Default;

    public JsonObject ToJson() => [];
}
