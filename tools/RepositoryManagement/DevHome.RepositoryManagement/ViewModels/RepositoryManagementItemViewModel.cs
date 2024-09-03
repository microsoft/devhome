// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.Input;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementItemViewModel
{
    public string RepositoryName { get; set; }

    public string ClonePath { get; set; }

    public string LatestCommit { get; set; }

    public string Branch { get; set; }

    public bool IsHiddenFromPage { get; set; }

    [RelayCommand]
    public void OpenInFileExplorer()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void OpenInCMD()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void MoveRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void DeleteRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void MakeConfigurationFileWithThisRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void OpenFileExplorerToConfigurationsFolder()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void RemoveThisRepositoryFromTheList()
    {
        throw new NotImplementedException();
    }
}
