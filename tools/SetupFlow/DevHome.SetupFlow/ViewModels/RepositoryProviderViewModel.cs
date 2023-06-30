// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.ViewModels;
public partial class RepositoryProviderViewModel : ObservableObject
{
    public string ProviderName { get; private set; }

    public string DisplayName { get; private set; }

    public RepositoryProviderViewModel(string providerName, string displayName)
    {
        ProviderName = providerName;
        DisplayName = displayName;
    }
}
