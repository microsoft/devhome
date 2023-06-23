// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.ViewModels;
public partial class AddViaAccountViewModel : ObservableObject
{
    [ObservableProperty]
    private string _providerName;

    [ObservableProperty]
    private string _accountName;

    [ObservableProperty]
    private ObservableCollection<string> _selectedRepositories;

    [ObservableProperty]
    private bool _isFetchingRepos;

    [ObservableProperty]
    private RepositoryProviders _repositoryProviders;
}
