// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Environments.CustomControls;

public sealed partial class CardFlyout : MenuFlyout
{
    private const double MinimumWidthOfItems = 200;

    public CardFlyout()
    {
        InitializeComponent();
    }

    public ObservableCollection<OperationsViewModel> ItemsViewModels
    {
        get => (ObservableCollection<OperationsViewModel>)GetValue(ItemsViewModelsProperty);
        set => SetValue(ItemsViewModelsProperty, value);
    }

    private static void ItemsLoaded(DependencyObject dependencyObj, DependencyPropertyChangedEventArgs args)
    {
        var flyout = (CardFlyout)dependencyObj;
        flyout.FillOperations();
    }

    private void FillOperations()
    {
        Items.Clear();

        if (ItemsViewModels != null)
        {
            foreach (var item in ItemsViewModels)
            {
                var flyoutItem = new MenuFlyoutItem
                {
                    Text = item.Name,
                    Icon = new FontIcon { Glyph = item.IconGlyph },
                    Command = item.InvokeActionCommand,
                    MinWidth = MinimumWidthOfItems,
                };
                Items.Add(flyoutItem);
            }
        }
    }

    // Using a DependencyProperty as the backing store for ItemsViewModels.
    public static readonly DependencyProperty ItemsViewModelsProperty = DependencyProperty.Register(nameof(ItemsViewModels), typeof(List<OperationsViewModel>), typeof(CardFlyout), new PropertyMetadata(null, ItemsLoaded));
}
