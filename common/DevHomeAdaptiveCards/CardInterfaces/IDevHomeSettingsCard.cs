// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;

namespace DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;

/// <summary>
/// Represents a Dev Home settings card that can be rendered through an adaptive card.
/// </summary>
public interface IDevHomeSettingsCard : IAdaptiveCardElement
{
    /// <summary>
    /// gets or sets the subtitle of the card. The description naming is used because that is what
    /// is used in the Windows Community Toolkit settings cards. This is displayed below the header.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// gets or sets the header of the card. This is the bolded text at the top of the card.
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// gets or sets the icon base64 string that represents an image. This is the icon that is
    /// displayed to the left of the header.
    /// </summary>
    public string HeaderIcon { get; set; }

    // An element that is not expected to submit the adaptive card
    public IDevHomeSettingsCardNonSubmitAction? NonSubmitActionElement { get; set; }

    // An element that is expected to submit the adaptive card
    public IAdaptiveActionElement? SubmitActionElement { get; set; }
}
