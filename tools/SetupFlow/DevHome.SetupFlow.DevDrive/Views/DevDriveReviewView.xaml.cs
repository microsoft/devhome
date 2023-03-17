// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.DevDrive.Views;
public sealed partial class DevDriveReviewView : UserControl
{
    public DevDriveReviewView()
    {
        this.InitializeComponent();
    }

    public DevDriveReviewViewModel ViewModel => (DevDriveReviewViewModel)DataContext;
}
