// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.Views;

#nullable enable
public sealed partial class QuickstartPlaygroundView : UserControl
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuickstartPlaygroundView));

    private ContentDialog? _adaptiveCardContentDialog;

    public QuickstartPlaygroundViewModel ViewModel
    {
        get; set;
    }

    public QuickstartPlaygroundView()
    {
        ViewModel = Application.Current.GetService<QuickstartPlaygroundViewModel>();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        this.InitializeComponent();
        PromptCharacterCount.Text = $"0 / {CustomPrompt.MaxLength}";
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != null)
        {
            if (e.PropertyName == "IsLaunchDropDownVisible")
            {
                DropDownButtonFlyout.Items.Clear();
                foreach (var item in ViewModel.QuickStartProjectHosts)
                {
                    DropDownButtonFlyout.Items.Add(
                        new MenuFlyoutItem()
                        {
                            Text = item.DisplayName,
                            Command = ViewModel.LaunchProjectHostCommand,
                            CommandParameter = item,
                        });
                }
            }
            else if (e.PropertyName == "ProgressAdaptiveCardSession")
            {
                _ = ShowProgressAdaptiveCard();
            }
        }
    }

    private void FolderHierarchy_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is ExplorerItem item)
        {
            if (item.Type == ExplorerItem.ExplorerItemType.Folder)
            {
                ViewModel.GeneratedFileContent = string.Empty;
            }
            else
            {
                // TODO: should theoretically do this more efficiently instead of re-reading the file
                if (item.FullPath != null)
                {
                    ViewModel.GeneratedFileContent = File.ReadAllText(item.FullPath);
                }
            }
        }
    }

    private async void ExtensionProviderComboBox_Loading(FrameworkElement sender, object args)
    {
        // TODO: For a nicer UX, we should enable the user to pick a default provider so that they don't have to select it every time.
        await ViewModel.PopulateQuickstartProvidersComboBox();
    }

    private async void ExtensionProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.OnQuickstartSelectionChanged();
        await ShowExtensionInitializationUI();
    }

    private void NegativeFeedbackConfirmation_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NegativesGroupOne = false;
        ViewModel.NegativesGroupTwo = true;

        ViewModel.ProvideFeedback(false, negativeFeedbackTextBox.Text);
        negativeFeedbackTextBox.Text = string.Empty;
    }

    private void PositiveFeedbackConfirmation_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ProvideFeedback(true, positiveFeedbackTextBox.Text);
        positiveFeedbackTextBox.Text = string.Empty;

        ViewModel.PositivesGroupOne = false;
        ViewModel.PositivesGroupTwo = true;
    }

    private void NegativeFeedbackFlyout_Closed(object sender, object e)
    {
        ViewModel.NegativesGroupOne = true;
        ViewModel.NegativesGroupTwo = false;
    }

    private void PositiveFeedbackFlyout_Closed(object sender, object e)
    {
        ViewModel.PositivesGroupOne = true;
        ViewModel.PositivesGroupTwo = false;
    }

    private void NegCloseFlyout_Click(object sender, RoutedEventArgs e)
    {
        negativeFeedbackFlyout.Hide();
    }

    private void PosCloseFlyout_Click(object sender, RoutedEventArgs e)
    {
        positiveFeedbackFlyout.Hide();
    }

    public void OnAdaptiveCardSessionStopped(IExtensionAdaptiveCardSession2 cardSession, ExtensionAdaptiveCardSessionStoppedEventArgs data)
    {
        cardSession.Stopped -= OnAdaptiveCardSessionStopped;
        if (_adaptiveCardContentDialog is not null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _adaptiveCardContentDialog?.Hide();
                if (data.Result.Status == ProviderOperationStatus.Failure)
                {
                    ViewModel.ActiveQuickstartSelection = null;
                }
            });
        }
    }

    private async Task ShowAdaptiveCardOnContentDialog(QuickStartProjectAdaptiveCardResult adaptiveCardSessionResult)
    {
        if (adaptiveCardSessionResult == null)
        {
            // No adaptive card to show (i.e. no dependencies or AI initialization).
            return;
        }

        if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"Failed to show adaptive card. {adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
            return;
        }

        var adapativeCardController = adaptiveCardSessionResult.AdaptiveCardSession;
        var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
        var renderingService = Application.Current.GetService<AdaptiveCardRenderingService>();
        var renderer = await renderingService.GetRendererAsync();

        extensionAdaptiveCardPanel.Bind(adapativeCardController, renderer);
        extensionAdaptiveCardPanel.RequestedTheme = ActualTheme;

        adapativeCardController.Stopped += OnAdaptiveCardSessionStopped;

        _adaptiveCardContentDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Content = extensionAdaptiveCardPanel,
        };

        await _adaptiveCardContentDialog.ShowAsync();

        adapativeCardController.Dispose();
        _adaptiveCardContentDialog = null;
    }

    public async Task ShowExtensionInitializationUI()
    {
        if (ViewModel.ActiveQuickstartSelection is not null)
        {
            var adaptiveCardSessionResult = ViewModel.ActiveQuickstartSelection.CreateAdaptiveCardSessionForExtensionInitialization();
            await ShowAdaptiveCardOnContentDialog(adaptiveCardSessionResult);
        }
    }

    public async Task ShowProgressAdaptiveCard()
    {
        var progressAdaptiveCardSession = ViewModel.ProgressAdaptiveCardSession;
        if (progressAdaptiveCardSession == null)
        {
            return;
        }

        var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
        var renderingService = Application.Current.GetService<AdaptiveCardRenderingService>();
        var renderer = await renderingService.GetRendererAsync();

        extensionAdaptiveCardPanel.Bind(progressAdaptiveCardSession, renderer);
        extensionAdaptiveCardPanel.RequestedTheme = ActualTheme;

        extensionAdaptiveCardPanel.UiUpdate += (object? sender, FrameworkElement e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
               ProgressOutputScrollViewer.ScrollToVerticalOffset(ProgressOutputScrollViewer.ScrollableHeight);
            });
        };

        ProgressOutputScrollViewer.Content = extensionAdaptiveCardPanel;
    }

    private void CustomPrompt_TextChanged(object sender, TextChangedEventArgs e)
    {
        PromptCharacterCount.Text = $"{CustomPrompt.Text.Length} / {CustomPrompt.MaxLength}";
    }

    private void CustomPrompt_GotFocus(object sender, RoutedEventArgs e)
    {
        var promptTextBox = sender as TextBox;
        if (promptTextBox != null)
        {
            promptTextBox.SelectionStart = promptTextBox.Text.Length;
        }
    }
}
