// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.DevHomeAdaptiveCards.InputValues;

/// <summary>
/// Represents the current value of an ItemsView input element.
/// </summary>
public class ItemsViewInputValue : IAdaptiveInputValue
{
    private readonly ItemsView _itemsView;

    public ItemsViewInputValue(IAdaptiveInputElement input, ItemsView itemsView)
    {
        InputElement = input;
        _itemsView = itemsView;
    }

    /// <summary>
    /// Gets the current value of the input element. This is the index of the current item.
    /// </summary>
    public string CurrentValue => _itemsView.CurrentItemIndex.ToString(CultureInfo.InvariantCulture);

    // Error message unused at this time because Validation is not needed for this class
    public UIElement? ErrorMessage { get; set; }

    public IAdaptiveInputElement InputElement { get; set; }

    public void SetFocus()
    {
        _itemsView.Focus(FocusState.Keyboard);
    }

    // Validation is not needed for this class
    public bool Validate()
    {
        return true;
    }
}
