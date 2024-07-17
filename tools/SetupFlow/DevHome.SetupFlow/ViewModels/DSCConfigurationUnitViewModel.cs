// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.SetupFlow.ViewModels;

public partial class DSCConfigurationUnitViewModel : ObservableObject
{
    private readonly IDSCUnit _configurationUnit;

    [ObservableProperty]
    private DSCConfigurationUnitDetailsViewModel _details;

    public string Id => _configurationUnit.Id;

    public string Title => GetTitle();

    public string Type => _configurationUnit.Type;

    public string Description => _configurationUnit.Description;

    public string Intent => _configurationUnit.Intent;

    public string ModuleName => _configurationUnit.ModuleName;

    public IList<string> Dependencies => _configurationUnit.Dependencies;

    public IList<KeyValuePair<string, string>> Settings => _configurationUnit.Settings;

    public IList<KeyValuePair<string, string>> Metadata => _configurationUnit.Metadata;

    public DSCConfigurationUnitViewModel(IDSCUnit configurationUnit)
    {
        _configurationUnit = configurationUnit;
    }

    private string GetTitle()
    {
        if (!string.IsNullOrEmpty(Description))
        {
            return Description;
        }

        return $"{ModuleName}/{Type}";
    }

    [RelayCommand]
    private async Task OnLoadedAsync()
    {
        var result = await _configurationUnit.GetDetailsAsync();
        Details = new DSCConfigurationUnitDetailsViewModel(result);
    }
}
