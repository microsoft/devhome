// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DevHome.SetupFlow.Views;

public sealed partial class CloneRepoSummaryInformationView : UserControl
{
    public CloneRepoSummaryInformationViewModel ViewModel => (CloneRepoSummaryInformationViewModel)this.DataContext;

    public CloneRepoSummaryInformationView()
    {
        this.InitializeComponent();
    }
}
