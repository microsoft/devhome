// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.Controls;

public sealed partial class ElevatedButtonControl : UserControl
{
    public event PropertyChangedEventHandler? PropertyChanged;

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

    public ElevatedButtonControl()
    {
        this.InitializeComponent();
    }
}
