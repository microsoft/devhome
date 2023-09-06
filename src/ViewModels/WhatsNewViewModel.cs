// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Models;

namespace DevHome.ViewModels;

public class WhatsNewViewModel : ObservableObject
{
    public ObservableCollection<WhatsNewCard> Source { get; } = new ObservableCollection<WhatsNewCard>();

    public ObservableCollection<WhatsNewCard> BigSource { get; } = new ObservableCollection<WhatsNewCard>();

    public void AddCard(WhatsNewCard card)
    {
        Source.Add(card);
    }

    public void AddBigCard(WhatsNewCard card)
    {
        BigSource.Add(card);
    }

    public void OnNavigatedTo(object parameter)
    {
        Source.Clear();
        BigSource.Clear();
    }

    public void OnNavigatedFrom()
    {
    }
}
