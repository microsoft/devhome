// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class CloneRepoSummaryInformationView : UserControl
{
    public CloneRepoSummaryInformationViewModel ViewModel => (CloneRepoSummaryInformationViewModel)this.DataContext;

    public CloneRepoSummaryInformationView()
    {
        this.InitializeComponent();
    }
}
