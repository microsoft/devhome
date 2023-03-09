// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Models;

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
        Source.Add(new WhatsNewCard("Title", "This is the description", "https://microsoft.com"));
        Source.Add(new WhatsNewCard("Title 2", "This is the description 2", "https://microsoft.com"));
        Source.Add(new WhatsNewCard("Title 3", "This is the description 3", "https://microsoft.com"));
        Source.Add(new WhatsNewCard("Title 4", "This is the description 4", "https://microsoft.com"));
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
