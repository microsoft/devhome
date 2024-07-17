// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Input;

namespace DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;

public interface IDevHomeSettingsCardNonSubmitAction : IAdaptiveCardElement
{
    public string ActionText { get; }

    [RelayCommand]
    public Task InvokeActionAsync(object sender);
}
