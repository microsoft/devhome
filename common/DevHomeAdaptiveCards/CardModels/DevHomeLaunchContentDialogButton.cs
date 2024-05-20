// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;
using DevHome.Common.Views.AdaptiveCardViews;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public partial class DevHomeLaunchContentDialogButton : IDevHomeSettingsCardNonSubmitAction
{
    // Specific properties for DevHomeLaunchContentDialogButton
    public string ActionText { get; set; } = string.Empty;

    public IAdaptiveCardElement? DialogContent { get; set; }

    public static string AdaptiveElementType => "DevHome.LaunchContentDialogButton";

    // Properties for IAdaptiveActionElement
    public string ElementTypeString { get; set; } = AdaptiveElementType;

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

    public JsonObject? ToJson() => [];

    [RelayCommand]
    public async Task InvokeActionAsync(object sender)
    {
        var senderObj = sender as Button;
        if (DialogContent is not DevHomeContentDialogContent dialogContent || senderObj == null)
        {
            return;
        }

        var dialog = new ContentDialogWithNonInteractiveContent(dialogContent);

        dialog.XamlRoot = senderObj.XamlRoot;

        await dialog.ShowAsync();
    }
}
