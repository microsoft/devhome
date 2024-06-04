// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Controls;

public sealed partial class EditToolsControl : UserControl
{
    public EditToolsControl()
    {
        InitializeComponent();
        EnableUnregisterButton();
    }

    private void UnregisterToolButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = ToolsDataGrid.SelectedItems;
        for (var i = selectedItems.Count - 1; i >= 0; i--)
        {
            if (selectedItems[i] is ExternalTool tool)
            {
                ExternalToolsHelper.Instance.RemoveExternalTool(tool);
            }
        }

        EnableUnregisterButton();
    }

    private void EnableUnregisterButton()
    {
        if (ToolsDataGrid.SelectedItems is not null && ToolsDataGrid.SelectedItems.Count > 0)
        {
            UnregisterToolButton.IsEnabled = true;
        }
        else
        {
            UnregisterToolButton.IsEnabled = false;
        }
    }

    private void ToolsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EnableUnregisterButton();
    }
}
