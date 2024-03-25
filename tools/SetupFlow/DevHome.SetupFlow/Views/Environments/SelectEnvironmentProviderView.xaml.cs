// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class SelectEnvironmentProviderView : UserControl
{
    public SelectEnvironmentProviderViewModel ViewModel => (SelectEnvironmentProviderViewModel)this.DataContext;

    public SelectEnvironmentProviderView()
    {
        this.InitializeComponent();
    }
}
