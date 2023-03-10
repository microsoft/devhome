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
    public ICommand ItemClickCommand
    {
        get;
    }

    public ObservableCollection<WhatsNewCard> Source { get; } = new ObservableCollection<WhatsNewCard>();

    public WhatsNewViewModel()
    {
        //ItemClickCommand = new RelayCommand<SampleOrder>(OnItemClick);

        //stringResource.GetLocalized("")

        //var xamlReader = XamlReader.LoadWithInitialTemplateValidation(File.ReadAllText("DataResources\\WhatsNewPageData.xaml"));
        //var x = 1;
    }

    public void AddCard(WhatsNewCard card)
    {
        Source.Add(card);
    }

    public async void OnNavigatedTo(object parameter)
    {
        Source.Clear();

        // TODO: Replace with real data.
        //var data = await _sampleDataService.GetContentGridDataAsync();
        //foreach (var item in data)
        //{
        //    Source.Add(item);
        //}

        
    }

    public void OnNavigatedFrom()
    {
    }

    //private void OnItemClick(SampleOrder? clickedItem)
    //{
    //    if (clickedItem != null)
    //    {
    //        _navigationService.SetListDataItemForNextConnectedAnimation(clickedItem);
    //        _navigationService.NavigateTo(typeof(WhatsNewDetailViewModel).FullName!, clickedItem.OrderID);
    //    }
    //}
}
