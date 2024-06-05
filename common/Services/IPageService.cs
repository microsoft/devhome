// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Services;

public interface IPageService
{
    Type GetPageType(string key);

    public void Configure<T_VM, T_V>()
        where T_VM : ObservableObject
        where T_V : Page;
}
