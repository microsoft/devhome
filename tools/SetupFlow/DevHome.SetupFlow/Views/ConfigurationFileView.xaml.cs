// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace DevHome.SetupFlow.Views;

public sealed partial class ConfigurationFileView : UserControl
{
    public ConfigurationFileViewModel ViewModel => (ConfigurationFileViewModel)DataContext;

    public ConfigurationFileView()
    {
        this.InitializeComponent();
    }

    private void SetUpButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Align the checked state with the enabled state.
        SetUpButton.IsChecked = SetUpButton.IsEnabled;
    }

    private void SetUpButton_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
    {
        // When the toggle button is invoked, its state is changed.
        // Always mark the toggle button as checked to ensure it visually looks toggled ON.
        SetUpButton.IsChecked = true;
    }

    private async void Dependency_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        var textBlock = sender?.ContentStart?.VisualParent as TextBlock;
        if (textBlock?.Tag is string unitId && !string.IsNullOrEmpty(unitId))
        {
            for (var i = 0; i < ConfigurationUnitSections.Items.Count; ++i)
            {
                var listViewItem = ConfigurationUnitSections.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                var expander = listViewItem?.ContentTemplateRoot as Expander;
                if (expander != null && expander.Tag?.ToString() == unitId)
                {
                    expander.IsExpanded = true;
                    expander.Focus(FocusState.Programmatic);
                    await ConfigurationUnitSections.SmoothScrollIntoViewWithIndexAsync(i);
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Represents a configuration unit data entry.
/// </summary>
public sealed class ConfigurationUnitDataEntry
{
    public string Key { get; set; }

    public string Value { get; set; }
}
