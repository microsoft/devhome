// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents.SetupFlow.QuickstartPlayground;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

#nullable enable

public partial class QuickstartPlaygroundViewModel : SetupPageViewModelBase
{
    public class FolderComboBoxItem
    {
        public string? DisplayFolderOutput
        {
            get; set;
        }

        public string? FullFolderOutput
        {
            get; set;
        }
    }

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuickstartPlaygroundViewModel));

    private readonly IQuickStartProjectService _quickStartProjectService;

    private readonly ILocalSettingsService _localSettingsService;

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private readonly ObservableCollection<ExplorerItem> _dataSource = new();

    private IQuickStartProjectGenerationOperation _quickStartProjectGenerationOperation = null!;

    private QuickStartProjectResult _quickStartProject = null!;

    [ObservableProperty]
    private bool _showExamplePrompts = false;

    [ObservableProperty]
    private bool _showPrivacyAndTermsLink = false;

    [ObservableProperty]
    private bool _enableQuickstartProjectCombobox = true;

    [ObservableProperty]
    private bool _isQuickstartProjectComboboxExpanded = false;

    [ObservableProperty]
    private IQuickStartProjectHost[] _quickStartProjectHosts = [];

    [ObservableProperty]
    private bool _isLaunchButtonVisible = true;

    [ObservableProperty]
    private bool _isLaunchDropDownVisible = false;

    [ObservableProperty]
    private string _launchButtonText = string.Empty;

    public ObservableCollection<ExplorerItem> DataSource => _dataSource;

    public ObservableCollection<QuickStartProjectProvider> QuickstartProviders { get; private set; } = [];

    [ObservableProperty]
    private string _samplePromptOne = string.Empty;

    [ObservableProperty]
    private string _samplePromptTwo = string.Empty;

    [ObservableProperty]
    private string _samplePromptThree = string.Empty;

    [ObservableProperty]
    private Uri? _privacyUri = default;

    [ObservableProperty]
    private Uri? _termsUri = default;

    [ObservableProperty]
    private string _outputFolderRoot = Path.Combine(Path.GetTempPath(), "DevHomeQuickstart");

    private string _outputFolderForCurrentPrompt = string.Empty;

    [ObservableProperty]
    private QuickStartProjectProvider? _activeQuickstartSelection;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCodespaceCommand))]
    private string? _customPrompt;

    [ObservableProperty]
    private string? _promptTextBoxPlaceholder;

    [ObservableProperty]
    private bool _isFileViewVisible = false;

    [ObservableProperty]
    private bool _isProgressOutputVisible = false;

    [ObservableProperty]
    private bool _isPromptTextBoxReadOnly = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCodespaceCommand))]
    private bool _areDependenciesPresent = false;

    [ObservableProperty]
    private string? _progressMessage;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private IExtensionAdaptiveCardSession2? _progressAdaptiveCardSession = null;

    // The four properties below are used to track the visibility state of the five controls
    // that make up the positive and negative feedback flyouts. These states are set in the "submit"
    // and "close" handlers. It turns out that the states can be completely represented by two
    // booleans, so each flyout has its own group that the View will data-bind to.
    [ObservableProperty]
    private bool _negativesGroupOne = true;

    [ObservableProperty]
    private bool _negativesGroupTwo = false;

    [ObservableProperty]
    private bool _positivesGroupOne = true;

    [ObservableProperty]
    private bool _positivesGroupTwo = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchProjectHostCommand), nameof(SaveProjectCommand))]
    private bool _enableProjectButtons = false;

    public QuickstartPlaygroundViewModel(
        ISetupFlowStringResource stringResource,
        IQuickStartProjectService quickStartProjectService,
        ILocalSettingsService localSettingsService,
        SetupFlowOrchestrator orchestrator)
        : base(stringResource, orchestrator)
    {
        IsStepPage = false;

        _quickStartProjectService = quickStartProjectService;
        _localSettingsService = localSettingsService;

        // Placeholder launch text while button is disabled.
        LaunchButtonText = StringResource.GetLocalized(StringResourceKey.QuickstartPlaygroundLaunchButton, string.Empty);
    }

    [RelayCommand(CanExecute = nameof(EnableProjectButtons))]
    public Task SaveProject()
    {
        return Task.Run(async () =>
        {
            TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundSaveProjectClicked");

            // TODO: Replace with WindowSaveFileDialog
            var folderPicker = new FolderPicker();
            var hWnd = Application.Current.GetService<WindowEx>().GetWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);
            folderPicker.FileTypeFilter.Add("*");

            var location = await folderPicker.PickSingleFolderAsync();
            if (!string.IsNullOrWhiteSpace(location?.Path))
            {
                CopyDirectory(_outputFolderForCurrentPrompt, location.Path);
                TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundSaveProjectCompleted");
            }
        });
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (dir.Exists)
        {
            // Cache directories before we start copying
            var dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (var file in dir.GetFiles())
            {
                var targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // Copy subdirectories
            foreach (var subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }

    public async Task<StorageFolder> GetOutputFolder()
    {
        // Ensure we're starting from a clean state
        DeleteDirectoryContents(OutputFolderRoot);

        var outputFolderForCurrentPrompt = Path.Combine(OutputFolderRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));

        if (!Directory.Exists(outputFolderForCurrentPrompt))
        {
            Directory.CreateDirectory(outputFolderForCurrentPrompt);
        }
        else
        {
            throw new IOException("Directory already exists.");
        }

        return await StorageFolder.GetFolderFromPathAsync(outputFolderForCurrentPrompt);
    }

    private void DeleteDirectoryContents(string folder)
    {
        // This is best-effort, so we won't block on failing to delete a prior project
        try
        {
            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            // Delete all subdirectories and their contents
            var subDirs = Directory.GetDirectories(folder);
            foreach (var dir in subDirs)
            {
                Directory.Delete(dir, true);
            }
        }
        catch (Exception ex)
        {
            TelemetryFactory.Get<ITelemetry>().LogException("QuickstartPlaygroundDeleteDirectoryContents", ex);
        }
    }

    public void SetUpFileView()
    {
        var explorerItems = CreateExplorerItem(_outputFolderForCurrentPrompt);

        if (explorerItems != null)
        {
            DataSource.Clear();
            DataSource.Add(explorerItems);
        }
        else
        {
            throw new ArgumentNullException("Error creating explorer items.");
        }

        IsFileViewVisible = true;
    }

    public static ExplorerItem? CreateExplorerItem(string path)
    {
        ExplorerItem? item = null;
        try
        {
            item = new ExplorerItem
            {
                Name = Path.GetFileName(path),
                Type = File.GetAttributes(path).HasFlag(System.IO.FileAttributes.Directory) ? ExplorerItem.ExplorerItemType.Folder : ExplorerItem.ExplorerItemType.File,
                FullPath = path,
            };

            if (item.Type == ExplorerItem.ExplorerItemType.Folder)
            {
                foreach (var subFolderPathString in Directory.GetDirectories(path))
                {
                    var subDirectory = CreateExplorerItem(subFolderPathString);
                    if (subDirectory != null)
                    {
                        item.Children.Add(subDirectory);
                    }
                    else
                    {
                        throw new ArgumentNullException($"Failed to add an ExplorerItem for {subFolderPathString}, CreateExplorerItem generated a null item");
                    }
                }

                foreach (var subPath in Directory.GetFiles(path))
                {
                    item.Children.Add(new ExplorerItem
                    {
                        Name = Path.GetFileName(subPath),
                        Type = ExplorerItem.ExplorerItemType.File,
                        FullPath = subPath,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        return item;
    }

    [RelayCommand]
    public async Task PopulateQuickstartProvidersComboBox()
    {
        var providers = await _quickStartProjectService.GetQuickStartProjectProvidersAsync();
        foreach (var provider in providers)
        {
            ArgumentNullException.ThrowIfNull(provider);
            QuickstartProviders.Add(provider);
        }

        // If there are no providers, update the placeholder text and return
        if (QuickstartProviders.Count == 0)
        {
            _log.Information("No installed Quickstart providers detected");
            PromptTextBoxPlaceholder = StringResource.GetLocalized("QuickstartPlaygroundNoProviderInstalled");
            return;
        }

        // Check to see if there's already a previous provider selected. If so, we will automatically set it.
        // This will raise the selection changed event and trigger the OnQuickstartSelectionChanged method.
        if (_localSettingsService.HasSettingAsync("QuickstartPlaygroundSelectedProvider").Result)
        {
            // Check to see if that DisplayName is in the list of providers
            var defaultProviderDisplayName = _localSettingsService.ReadSettingAsync<string>("QuickstartPlaygroundSelectedProvider").Result;
            var selectedProvider = QuickstartProviders.FirstOrDefault(p => p.DisplayName == defaultProviderDisplayName);
            if (selectedProvider != null)
            {
                _log.Information("Automatically using previously-selected Quickstart extension provider");
                ActiveQuickstartSelection = selectedProvider;
                PromptTextBoxPlaceholder = StringResource.GetLocalized("QuickstartPlaygroundGenerationPromptPlaceholder");
            }
            else
            {
                _log.Information("Previously-selected provider not found in provider list (maybe it was uninstalled or the extension was turned off)");
                ConfigureForProviderSelection();
            }
        }
        else
        {
            _log.Information("No prior provider selection found.");
            ConfigureForProviderSelection();
        }
    }

    private void ConfigureForProviderSelection()
    {
        _log.Information("Asking user to select a new provider");
        IsQuickstartProjectComboboxExpanded = true;
        PromptTextBoxPlaceholder = StringResource.GetLocalized("QuickstartPlaygroundSelectProvider");
    }

    public void UpdateProgress(QuickStartProjectProgress progress)
    {
        ProgressMessage = progress.DisplayStatus;
        ProgressValue = (int)(progress.Progress * 100);
        var adaptiveCardSession = _quickStartProjectGenerationOperation.AdaptiveCardSession;
        if (adaptiveCardSession != ProgressAdaptiveCardSession)
        {
            ProgressAdaptiveCardSession = adaptiveCardSession;
            IsProgressOutputVisible = adaptiveCardSession != null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerateCodespace))]
    public async Task GenerateCodespace()
    {
        try
        {
            var userPrompt = CustomPrompt;

            // TODO: Replace this (and throughout) with proper diagnostics / logging
            ArgumentNullException.ThrowIfNullOrEmpty(userPrompt);
            ArgumentNullException.ThrowIfNull(ActiveQuickstartSelection);

            TelemetryFactory.Get<ITelemetry>().Log("QuickstartPlaygroundGenerateButtonClicked", LogLevel.Critical, new GenerateButtonClicked(userPrompt));

            // Ensure file view isn't visible (in the case where the user has previously run a Generate command
            IsFileViewVisible = false;

            // Temporarily turn off the provider combobox and ensure user cannot edit the prompt for the moment
            EnableQuickstartProjectCombobox = false;
            IsPromptTextBoxReadOnly = true;

            IProgress<QuickStartProjectProgress> progress = new Progress<QuickStartProjectProgress>(UpdateProgress);

            var outputFolder = await GetOutputFolder();
            _outputFolderForCurrentPrompt = outputFolder.Path;

            _quickStartProjectGenerationOperation = ActiveQuickstartSelection.CreateProjectGenerationOperation(userPrompt, outputFolder);
            _quickStartProject = await _quickStartProjectGenerationOperation.GenerateAsync().AsTask(progress);
            _quickStartProjectGenerationOperation = null!;
            if (_quickStartProject.Result.Status == ProviderOperationStatus.Success)
            {
                SetUpFileView();
                SetupLaunchButton();
                EnableProjectButtons = true;
                TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundGenerateSuccceded");
            }
            else
            {
                // TODO handle error scenario
                TelemetryFactory.Get<ITelemetry>().Log("QuickstartPlaygroundGenerateFailed", LogLevel.Critical, new ProjectGenerationErrorInfo(_quickStartProject.Result.DisplayMessage, _quickStartProject.Result.ExtendedError, _quickStartProject.Result.DiagnosticText));
            }
        }
        finally
        {
            // Re-enable the provider combobox and prompt textbox
            EnableQuickstartProjectCombobox = true;
            IsPromptTextBoxReadOnly = false;
        }
    }

    private void SetupLaunchButton()
    {
        QuickStartProjectHosts = _quickStartProject.ProjectHosts;

        IsLaunchButtonVisible = QuickStartProjectHosts.Length == 1;
        IsLaunchDropDownVisible = QuickStartProjectHosts.Length > 1;

        if (IsLaunchButtonVisible)
        {
            LaunchButtonText = StringResource.GetLocalized(StringResourceKey.QuickstartPlaygroundLaunchButton, QuickStartProjectHosts[0].DisplayName);
        }
    }

    [RelayCommand(CanExecute = nameof(EnableProjectButtons))]
    private async Task LaunchProjectHost(IQuickStartProjectHost? projectHost = null)
    {
        TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundLaunchProjectClicked");
        var projectHostToLaunch = projectHost ?? QuickStartProjectHosts[0];
        await Task.Run(projectHostToLaunch.Launch);
    }

    public void ProvideFeedback(bool isPositive, string feedback)
    {
        TelemetryFactory.Get<ITelemetry>().Log("QuickstartPlaygroundFeedbackSubmitted", LogLevel.Critical, new FeedbackSubmitted(isPositive, feedback));
        _quickStartProject?.FeedbackHandler?.ProvideFeedback(isPositive, feedback);
    }

    public bool CanGenerateCodespace()
    {
        return !string.IsNullOrEmpty(CustomPrompt) && ActiveQuickstartSelection != null;
    }

    [RelayCommand]
    public void CopyExamplePrompt(string selectedPrompt)
    {
        CustomPrompt = selectedPrompt;
    }

    [RelayCommand]
    public void OnQuickstartSelectionChanged()
    {
        if (ActiveQuickstartSelection != null)
        {
            var prompts = ActiveQuickstartSelection.SamplePrompts;

            // TODO: this needs to be more robust to potentially handle variable numbers of
            // prompts supplied by an extension. Right now, we're going to assume that we are
            // only dealing with three prompts and that extensions conform to this.
            if (prompts.Length == 3)
            {
                SamplePromptOne = prompts[0];
                SamplePromptTwo = prompts[1];
                SamplePromptThree = prompts[2];
            }
            else
            {
                _log.Error($"{ActiveQuickstartSelection.DisplayName} did not provide the expected number of sample prompts. Expected 3, but got {prompts.Length}");
            }

            TermsUri = ActiveQuickstartSelection.TermsOfServiceUri;
            PrivacyUri = ActiveQuickstartSelection.PrivacyPolicyUri;

            // Reset state
            ShowExamplePrompts = true;
            CustomPrompt = string.Empty;
            IsPromptTextBoxReadOnly = false;
            ShowPrivacyAndTermsLink = true;
            _outputFolderForCurrentPrompt = string.Empty;
            DataSource.Clear();
            IsFileViewVisible = false;
            IsLaunchDropDownVisible = false;
            IsLaunchButtonVisible = true;
            EnableProjectButtons = false;
            IsQuickstartProjectComboboxExpanded = false;
            PromptTextBoxPlaceholder = StringResource.GetLocalized("QuickstartPlaygroundGenerationPromptPlaceholder");

            // Update our setting to indicate the user preference
            _localSettingsService.SaveSettingAsync("QuickstartPlaygroundSelectedProvider", ActiveQuickstartSelection.DisplayName);

            _log.Information("Completed setup work for extension selection");
        }
    }
}

public class ExplorerItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public enum ExplorerItemType
    {
        Folder,
        File,
    }

    public string? Name
    {
        get; set;
    }

    public ExplorerItemType Type
    {
        get; set;
    }

    public string? FullPath
    {
        get; set;
    }

    private ObservableCollection<ExplorerItem>? children;

    public ObservableCollection<ExplorerItem> Children
    {
        get
        {
            children ??= new ObservableCollection<ExplorerItem>();
            return children;
        }
        set => children = value;
    }

    private bool isExpanded;

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded != value)
            {
                isExpanded = value;
                NotifyPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate
    {
        get; set;
    }

    public DataTemplate? FileTemplate
    {
        get; set;
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        var explorerItem = (ExplorerItem)item;
        return explorerItem.Type == ExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
    }
}
