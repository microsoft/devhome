// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.WebView2Tool.ViewModels;

public partial class WebView2ToolViewModel : ObservableObject
{
    [ObservableProperty]
    private string _webMessageReceived;

    [ObservableProperty]
    private int _numberOfPages = 1;

    [ObservableProperty]
    private int _finalProgress = 100;

    [ObservableProperty]
    private int _progress = 0;

    internal void IncreaseProgress()
    {
        Progress += 100 / NumberOfPages;
    }

    internal void DecreaseProgress()
    {
        Progress -= 100 / NumberOfPages;
    }
}
