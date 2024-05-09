// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Input;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Utilities.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace DevHome.Utilities.Views;

public sealed partial class UtilitiesMainPageView : ToolPage
{
    public override string ShortName => "Utilities";

    public UtilitiesMainPageViewModel ViewModel { get; }

    public ICommand OpenNewWindowCommand { get; private set; }

    public UtilitiesMainPageView()
    {
        ViewModel = Application.Current.GetService<UtilitiesMainPageViewModel>();
        this.InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Focus on the first focusable element inside the shell content
        var element = FocusManager.FindFirstFocusableElement(ParentContainer);
        if (element != null)
        {
            await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
        }
    }
}
