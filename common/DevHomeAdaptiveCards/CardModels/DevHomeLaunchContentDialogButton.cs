// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.Views.AdaptiveCardViews;
using Microsoft.UI.Xaml;
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

        var rendererService = Application.Current.GetService<AdaptiveCardRenderingService>();
        var renderer = await rendererService.GetRendererAsync();
        var dialog = new ContentDialog();

        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;
        var card = renderer.RenderAdaptiveCardFromJsonString(dialogContent.ContentDialogInternalAdaptiveCardJson?.Stringify() ?? string.Empty);

        dialog.Title = dialogContent.Title;
        dialog.PrimaryButtonText = dialogContent.PrimaryButtonText;
        dialog.Content = card.FrameworkElement;
        dialog.SecondaryButtonText = dialogContent.SecondaryButtonText;

        dialog.XamlRoot = senderObj.XamlRoot;

        await dialog.ShowAsync();
    }
}
