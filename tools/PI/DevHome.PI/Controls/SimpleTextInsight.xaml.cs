// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Controls;

public sealed partial class SimpleTextInsightControl : UserControl
{
    private string _description = string.Empty;

    public string Description
    {
        get => _description;

        set
        {
            _description = value;
            TextField.Text = value;
        }
    }

    public SimpleTextInsightControl()
    {
        this.InitializeComponent();
    }
}
