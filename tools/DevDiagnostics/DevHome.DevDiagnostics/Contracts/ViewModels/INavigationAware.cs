// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.DevDiagnostics.Contracts.ViewModels;

// Similar to DevHome.Contracts.ViewModels.INavigationAware
public interface INavigationAware
{
    void OnNavigatedTo(object parameter);

    void OnNavigatedFrom();
}
