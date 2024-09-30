// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.System;

namespace DevHome.SetupFlow.Views;

#nullable enable
public sealed partial class QuickstartPlaygroundView : UserControl
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuickstartPlaygroundView));

    private readonly IThemeSelectorService _themeSelector;

    private ContentDialog? _adaptiveCardContentDialog;

    /// <summary>
    /// Used to keep track of the current adaptive card session.
    /// The session should not be reloaded in the same QSP instance with the same provider.
    /// If it is, Content of the content dialog might change depending on
    /// user actions between reloads of _adaptiveCardSession2.
    /// </summary>
    private IExtensionAdaptiveCardSession2? _adaptiveCardSession2;

    /// <summary>
    ///  Because QSP can use different providers, the session *should* be reloaded
    ///  if the provider changes.
    /// </summary>
    private bool _shouldReloadAdaptiveCardSession;

    public QuickstartPlaygroundViewModel ViewModel
    {
        get; set;
    }

    public QuickstartPlaygroundView()
    {
        _themeSelector = Application.Current.GetService<IThemeSelectorService>();
        _themeSelector.ThemeChanged += OnThemeChanged;
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
        _shouldReloadAdaptiveCardSession = true;
        ViewModel.OnQuickstartSelectionChanged();
        await ShowExtensionInitializationUI();
        _shouldReloadAdaptiveCardSession = false;
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

    private async Task ShowAdaptiveCardOnContentDialog()
    {
        var extensionAdaptiveCardPanel = await SetUpAdaptiveCardAsync();
        if (extensionAdaptiveCardPanel == null || _adaptiveCardSession2 == null)
        {
            // No adaptive card to show (i.e. no dependencies or AI initialization).
            return;
        }

        _adaptiveCardSession2.Stopped += OnAdaptiveCardSessionStopped;

        _adaptiveCardContentDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Content = extensionAdaptiveCardPanel,

            // Set the theme of the content dialog box
            RequestedTheme = _themeSelector.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light,
        };

        await _adaptiveCardContentDialog.ShowAsync();

        _adaptiveCardSession2.Dispose();
        _adaptiveCardContentDialog = null;
    }

    /// <summary>
    /// Makes the adaptive card panel to use in the content dialog.  Will check if
    /// an adaptive card session is made and if not, make one.
    /// </summary>
    /// <returns>An awatible task that has an ExtensionAdaptiveCardPanel.  Returns null if
    /// the session can not be made.</returns>
    private async Task<ExtensionAdaptiveCardPanel?> SetUpAdaptiveCardAsync()
    {
        var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
        var renderingService = Application.Current.GetService<AdaptiveCardRenderingService>();
        var renderer = await renderingService.GetRendererAsync();

        // Make the session if not already.
        SaveAdaptiveCardSession();
        if (_adaptiveCardSession2 == null)
        {
            return null;
        }

        extensionAdaptiveCardPanel.Bind(_adaptiveCardSession2, renderer);
        extensionAdaptiveCardPanel.RequestedTheme = _themeSelector.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;

        return extensionAdaptiveCardPanel;
    }

    /// <summary>
    /// Changed the dialog theme if the windows theme changes.  This is done in 3 steps.
    /// 1. Reload the renderer.  This forces the renderer to use the correct host config file.
    /// 2. Reload the adaptive card (Adaptive card content will not change themes unless re-loaded)
    /// 3. Replace the Content of the content dialog.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="newRequestedTheme">The new theme</param>
    private async void OnThemeChanged(object? sender, ElementTheme newRequestedTheme)
    {
        RequestedTheme = newRequestedTheme;

        if (_adaptiveCardContentDialog == null)
        {
            return;
        }

        if (_adaptiveCardSession2 == null)
        {
            return;
        }

        var extensionPanel = await SetUpAdaptiveCardAsync();
        if (extensionPanel == null)
        {
            return;
        }

        _adaptiveCardContentDialog.Content = extensionPanel;
        _adaptiveCardContentDialog.RequestedTheme = _themeSelector.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;

        // Resetting Content breaks all events.  Re-hook the close button event.
        _adaptiveCardSession2.Stopped += OnAdaptiveCardSessionStopped;
    }

    public async Task ShowExtensionInitializationUI()
    {
        if (ViewModel.ActiveQuickstartSelection is not null)
        {
            await ShowAdaptiveCardOnContentDialog();
        }
    }

    /// <summary>
    /// Gets the adaptive card session from the provider and saves it.
    /// Session will not reload if
    /// 1. The provider did not change.
    /// 2. The session is not null.
    /// </summary>
    private void SaveAdaptiveCardSession()
    {
        if (!_shouldReloadAdaptiveCardSession || _adaptiveCardSession2 is not null)
        {
            return;
        }

        var adaptiveCardSessionResult = ViewModel.ActiveQuickstartSelection?.CreateAdaptiveCardSessionForExtensionInitialization(ViewModel.ActivityId);
        if (adaptiveCardSessionResult == null)
        {
            _adaptiveCardSession2 = null;
            return;
        }

        if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"Failed to show adaptive card. {adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
            _adaptiveCardSession2 = null;
            return;
        }

        _adaptiveCardSession2 = adaptiveCardSessionResult.AdaptiveCardSession;
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
        extensionAdaptiveCardPanel.RequestedTheme = _themeSelector.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;

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

    private async void CustomPrompt_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ViewModel.ShowExamplePrompts = false;

            var textBox = sender as TextBox;

            if (textBox != null)
            {
                var message = textBox.Text;

                if (message != null && message != string.Empty)
                {
                    ViewModel.ChatMessages.Add(new ChatStyleMessage
                    {
                        Name = message,
                        Type = ChatStyleMessage.ChatMessageItemType.Request,
                    });

                    await ViewModel.GenerateChatStyleCompetions(message);
                }
            }
        }
    }
}
