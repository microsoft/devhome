// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Models;

namespace DevHome.ViewModels;

public partial class WhatsNewViewModel : ObservableObject
{
    public ObservableCollection<WhatsNewCard> Source { get; } = new ObservableCollection<WhatsNewCard>();

    public ObservableCollection<WhatsNewCard> BigSource { get; } = new ObservableCollection<WhatsNewCard>();

    public int NumberOfBigCards { get; set; }

    private readonly INavigationService _navigationService;

    public WhatsNewViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void AddCard(WhatsNewCard card)
    {
        Source.Add(card);
    }

    public void AddBigCard(WhatsNewCard card)
    {
        BigSource.Add(card);
    }

    // When the width is too small, the big cards should go into the normal sized cards collection.
    // To do this, we first merge all the cards together in a List, then we sort them by priority,
    // and finally throw them back in the normal sized cards collection.
    public void SwitchToSmallerView()
    {
        if (BigSource.Count == 0)
        {
            return;
        }

        lock (Source)
        {
            lock (BigSource)
            {
                var cards = new List<WhatsNewCard>();
                foreach (var card in BigSource)
                {
                    cards.Add(card);
                }

                BigSource.Clear();

                foreach (var card in Source)
                {
                    cards.Add(card);
                }

                Source.Clear();

                cards.Sort((card1, card2) => card1.Priority - card2.Priority);

                foreach (var card in cards)
                {
                    Source.Add(card);
                }
            }
        }
    }

    // When the widget is large enough for the big cards, we get all the big cards from
    // the normal sized cards collection, order by priority, and throw them back into
    // the big cards collection.
    public void SwitchToLargerView()
    {
        if (BigSource.Count != 0)
        {
            return;
        }

        lock (Source)
        {
            lock (BigSource)
            {
                var cards = new List<WhatsNewCard>();
                foreach (var card in Source)
                {
                    if (card.IsBig == true)
                    {
                        cards.Add(card);
                    }
                }

                foreach (var card in cards)
                {
                    Source.Remove(card);
                }

                cards.Sort((card1, card2) => card1.Priority - card2.Priority);

                foreach (var card in cards)
                {
                    BigSource.Add(card);
                }
            }
        }
    }

    [RelayCommand]
    private void HeaderButtonNavigate()
    {
        _navigationService.NavigateTo(typeof(SetupFlow.ViewModels.SetupFlowViewModel).FullName!);
    }
}
