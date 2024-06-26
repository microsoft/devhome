// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace DevHome.SetupFlow.Views;

public sealed partial class SummaryView : UserControl
{
    private readonly UISettings _uiSettings = new();

    private double _textScale = 1.0;

    private const double BaseWidth = 550;

    public SummaryView()
    {
        this.InitializeComponent();

        _textScale = _uiSettings.TextScaleFactor;
        _uiSettings.TextScaleFactorChanged += HandleTextScaleFactorChanged;
        ParentUniformGrid.MinWidth = BaseWidth * _textScale;
    }

    public SummaryViewModel ViewModel => (SummaryViewModel)this.DataContext;

    private void HandleTextScaleFactorChanged(UISettings sender, object args)
    {
        Application.Current.GetService<DispatcherQueue>().EnqueueAsync(() =>
        {
            _textScale = sender.TextScaleFactor;
            ParentUniformGrid.MinWidth = BaseWidth * _textScale;
            InvalidateMeasure();
        });
    }
}
