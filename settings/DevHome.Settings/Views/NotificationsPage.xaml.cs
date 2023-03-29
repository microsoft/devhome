// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Labs.WinUI;
using DevHome.Common;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;

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
