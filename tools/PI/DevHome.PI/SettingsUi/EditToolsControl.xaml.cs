// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.SettingsUi;

public sealed partial class EditToolsControl : UserControl
{
    public EditToolsControl()
    {
        InitializeComponent();
        ToolsDataGrid.ItemsSource = ExternalToolsHelper.Instance.ExternalTools;
    }

    private void DeleteToolButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = ToolsDataGrid.SelectedItem;

        if (selectedItem is ExternalTool)
        {
            ExternalToolsHelper.Instance.RemoveExternalTool((ExternalTool)selectedItem);
        }
    }
}
