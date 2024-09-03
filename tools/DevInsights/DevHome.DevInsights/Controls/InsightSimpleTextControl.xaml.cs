// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DD.Controls;

public sealed partial class InsightSimpleTextControl : UserControl
{
    private string _description = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Description
    {
        get => _description;

        set
        {
            _description = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
        }
    }

    public InsightSimpleTextControl()
    {
        this.InitializeComponent();
    }
}
