// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class NotificationsPage : Page
{
    public NotificationsViewModel ViewModel
    {
        get;
    }

    public NotificationsPage()
    {
        ViewModel = new NotificationsViewModel();
        this.InitializeComponent();
    }
}
