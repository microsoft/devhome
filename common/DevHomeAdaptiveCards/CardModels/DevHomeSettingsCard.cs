// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class DevHomeSettingsCard : IDevHomeSettingsCard
{
    // These properties relate to the Windows Community Toolkit's SettingsCard control.
    // We'll allow extensions to provide the data for the SettingsCard control from an Adaptive Card.
    // Then we'll render the actual SettingsCard control in the DevHome app.
    public string Description { get; set; } = string.Empty;

    public string Header { get; set; } = string.Empty;

    public string HeaderIcon { get; set; } = string.Empty;

    public ImageIcon? HeaderIconImage { get; set; }

    public IDevHomeSettingsCardNonSubmitAction? NonSubmitActionElement { get; set; }

    public IAdaptiveActionElement? SubmitActionElement { get; set; }

    // Properties for IAdaptiveCardElement
    public string ElementTypeString { get; set; } = AdaptiveElementType;

    public static string AdaptiveElementType => "DevHome.SettingsCard";

    public JsonObject AdditionalProperties { get; set; } = new();

    public ElementType ElementType { get; set; } = ElementType.Custom;

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
