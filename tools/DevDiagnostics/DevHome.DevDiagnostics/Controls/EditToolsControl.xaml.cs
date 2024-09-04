// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevDiagnostics.Controls;

public sealed partial class EditToolsControl : UserControl
{
    private readonly ExternalToolsHelper _externalTools;

    public EditToolsControl()
    {
        _externalTools = Application.Current.GetService<ExternalToolsHelper>();
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
                _externalTools.RemoveExternalTool(tool);
            }
        }

        EnableUnregisterButton();
    }

    private void EnableUnregisterButton()
    {
        UnregisterToolButton.IsEnabled = ToolsDataGrid.SelectedItems?.Count > 0;
    }

    private void ToolsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EnableUnregisterButton();
    }
}
