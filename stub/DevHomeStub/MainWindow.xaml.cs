// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Runtime.Versioning;
using System.Windows;
using DevHome.Stub.Controls;

namespace DevHome.Stub;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
[SupportedOSPlatform("Windows10.0.21200.0")]
public partial class MainWindow : WinAutomationWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        var protocolHandlerArguments = Application.Current.Properties.Contains("protocolArgs") ? Application.Current.Properties["protocolArgs"].ToString() : default;
        DataContext = _viewModel = new MainViewModel(protocolHandlerArguments);
        Loaded += OnLoaded;

        InitializeComponent();
    }

    private void OnLoaded(object sender, EventArgs e)
    {
        Loaded -= OnLoaded; // First load only.

        _viewModel.Initialize();
    }
}
