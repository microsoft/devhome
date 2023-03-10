// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Models;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Views;

public sealed partial class WhatsNewPage : Page
{
    public WhatsNewViewModel ViewModel
    {
        get;
    }

    public WhatsNewPage()
    {
        ViewModel = Application.Current.GetService<WhatsNewViewModel>();
        InitializeComponent();

        var whatsNewCards = FeaturesContainer.Resources
            .Where((item) => item.Value.GetType() == typeof(WhatsNewCard))
            .Select(card => card.Value as WhatsNewCard);

        foreach (var card in whatsNewCards)
        {
            if (card is null)
            {
               continue;
            }

            ViewModel.AddCard(card);
        }
    }
}
