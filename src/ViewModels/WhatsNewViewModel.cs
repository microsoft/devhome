// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Models;
using DevHome.SetupFlow.Common.Services;
using Microsoft.UI.Xaml.Markup;

namespace DevHome.ViewModels;

public class WhatsNewViewModel : ObservableRecipient
{
    public ObservableCollection<WhatsNewCard> Source { get; } = new ObservableCollection<WhatsNewCard>();

    public void AddCard(WhatsNewCard card)
    {
        Source.Add(card);
    }

    public void OnNavigatedTo(object parameter)
    {
        Source.Clear();
    }

    public void OnNavigatedFrom()
    {
    }
}
