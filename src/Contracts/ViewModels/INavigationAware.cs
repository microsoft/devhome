// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Contracts.ViewModels;

public interface INavigationAware
{
    void OnNavigatedTo(object parameter);

    void OnNavigatedFrom();
}
