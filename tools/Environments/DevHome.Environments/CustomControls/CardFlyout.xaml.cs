// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        if (args.OldValue != null)
        {
            var oldOperationsViewModel = (INotifyCollectionChanged)args.OldValue;

            // Unsubscribe from CollectionChanged for the old collection
            oldOperationsViewModel.CollectionChanged -= flyout.OperationsViewModelCollectionChanged;
        }

        if (args.NewValue != null)
        {
            var newOperationsViewModel = (ObservableCollection<OperationsViewModel>)args.NewValue;

            // Subscribe to CollectionChanged for the new collection
            newOperationsViewModel.CollectionChanged += flyout.OperationsViewModelCollectionChanged;
        }

        flyout.FillOperations();
    }

    private void FillOperations()
    {
        Items.Clear();
        if (ItemsViewModels != null)
        {
            foreach (var item in ItemsViewModels)
            {
                Items.Add(CreateFlyoutItem(item));
            }
        }
    }

    private MenuFlyoutItem CreateFlyoutItem(OperationsViewModel viewModel)
    {
        return new MenuFlyoutItem
        {
            Text = viewModel.Name,
            Icon = new FontIcon { Glyph = viewModel.IconGlyph },
            Command = viewModel.InvokeActionCommand,
            MinWidth = MinimumWidthOfItems,
        };
    }

    private void OperationsViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs viewModelCollectionChangedArgs)
    {
        // Collection was cleared
        if (viewModelCollectionChangedArgs.Action == NotifyCollectionChangedAction.Reset)
        {
            Items.Clear();
        }

        if (viewModelCollectionChangedArgs.NewItems == null)
        {
            return;
        }

        if (viewModelCollectionChangedArgs.NewItems.Count > 0 && viewModelCollectionChangedArgs.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in viewModelCollectionChangedArgs.NewItems)
            {
                if (item is OperationsViewModel operationViewModels)
                {
                    Items.Add(CreateFlyoutItem(operationViewModels));
                }
            }
        }
    }

    // Using a DependencyProperty as the backing store for ItemsViewModels.
    public static readonly DependencyProperty ItemsViewModelsProperty = DependencyProperty.Register(nameof(ItemsViewModels), typeof(List<OperationsViewModel>), typeof(CardFlyout), new PropertyMetadata(null, ItemsLoaded));
}
