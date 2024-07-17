// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Customization.Views;

public sealed partial class GeneralSystemPage : DevHomePage
{
    public GeneralSystemViewModel ViewModel
    {
        get;
    }

    public GeneralSystemPage()
    {
        ViewModel = Application.Current.GetService<GeneralSystemViewModel>();
        this.InitializeComponent();
    }
}
