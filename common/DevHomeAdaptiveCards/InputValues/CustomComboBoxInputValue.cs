// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.DevHomeAdaptiveCards.InputValues;

public class CustomComboBoxInputValue : IAdaptiveInputValue
{
    private readonly ComboBox _comboBox;

    public CustomComboBoxInputValue(IAdaptiveInputElement input, ComboBox comboBox)
    {
        InputElement = input;
        _comboBox = comboBox;
    }

    /// <summary>
    /// Gets the current value of the input element. This is the index of the current item.
    /// </summary>
    public string CurrentValue => _comboBox.SelectedIndex.ToString(CultureInfo.InvariantCulture);

    public UIElement? ErrorMessage { get; set; }

    public IAdaptiveInputElement InputElement { get; set; }

    public void SetFocus()
    {
        _comboBox.Focus(FocusState.Keyboard);
    }

    public bool Validate()
    {
        return _comboBox.SelectedIndex >= 0;
    }
}
