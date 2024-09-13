// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.ComponentModel;
using DevHome.DevDiagnostics.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevDiagnostics.Controls;

public sealed partial class InsightForMissingFileProcessTerminationControl : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ElevationButtonTextString { get; } = CommonHelper.GetLocalizedString("InsightsMissingFileEnableLoaderSnapsButtonLabel");

    public string ElevationButtonToolTipString { get; } = CommonHelper.GetLocalizedString("InsightsMissingFileEnableLoaderSnapsButtonToolTip");

    private string _text = string.Empty;

    public string Text
    {
        get => _text;

        set
        {
            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    private System.Windows.Input.ICommand? _command;

    public System.Windows.Input.ICommand? Command
    {
        get => _command;

        set
        {
            _command = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Command)));
        }
    }

    public InsightForMissingFileProcessTerminationControl()
    {
        this.InitializeComponent();
    }
}
