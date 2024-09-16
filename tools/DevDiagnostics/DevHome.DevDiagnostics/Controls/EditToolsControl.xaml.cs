// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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
        List<ExternalTool> toolsToRemove = new();

        // First, grab all the tools we want to remove. The grid seems to have some unexpected behavior
        // with its list of selected items when you start removing them.
        foreach (var item in ToolsDataGrid.SelectedItems)
        {
            if (item is ExternalTool tool)
            {
                toolsToRemove.Add(tool);
            }
        }

        // And now remove all of the tools
        foreach (var tool in toolsToRemove)
        {
            _externalTools.RemoveExternalTool(tool);
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
