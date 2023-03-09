// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Views;

public sealed partial class WhatsNewPage : Page
{
    public WhatsNewViewModel ViewModel
    {
        get;
    }

    public WhatsNewPage()
    {
        ViewModel = Application.Current.GetService<WhatsNewViewModel>();
        InitializeComponent();
    }
}
