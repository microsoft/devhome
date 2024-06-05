// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using CommunityToolkit.Mvvm.Input;

namespace DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;

/// <summary>
/// Represents an action that can be invoked from a Dev Home settings card that does not submit the card. E.g launches a dialog.
/// </summary>
public interface IDevHomeSettingsCardNonSubmitAction : IAdaptiveCardElement
{
    /// <summary>
    /// Gets the text that is displayed on the action element. E.g button text.
    /// </summary>
    public string ActionText { get; }

    /// <summary>
    /// Invokes the action through a relay command
    /// </summary>
    /// <param name="sender">The UI object that the command originated from</param>
    [RelayCommand]
    public Task InvokeActionAsync(object sender);
}
