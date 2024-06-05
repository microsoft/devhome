// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.Views;

public sealed partial class ExperimentalFeaturesPage : DevHomePage
{
    public ExperimentalFeaturesViewModel ViewModel { get; }

    public ExperimentalFeaturesPage()
    {
        ViewModel = Application.Current.GetService<ExperimentalFeaturesViewModel>();
        this.InitializeComponent();
    }
}
