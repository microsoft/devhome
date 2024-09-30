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
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow.QuickstartPlayground;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

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

    private readonly ObservableCollection<ExplorerItem> _dataSource = new();

    private readonly ObservableCollection<ChatStyleMessage> _chatMessages = new();

    private readonly IExperimentationService _experimentationService;

    public Guid ActivityId { get; }

    private IQuickStartProjectGenerationOperation _quickStartProjectGenerationOperation = null!;

    private IQuickStartChatStyleGenerationOperation _quickStartChatStyleGeneration = null!;

    private QuickStartProjectResult _quickStartProject = null!;

    [ObservableProperty]
    private bool _isPromptValid = false;

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

    public ObservableCollection<ChatStyleMessage> ChatMessages => _chatMessages;

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
    [NotifyCanExecuteChangedFor(nameof(OpenReferenceSampleCommand))]
    private Uri? _referenceSampleUri = default;

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
    private string _generatedFileContent = string.Empty;

    [ObservableProperty]
    private bool _isProgressOutputVisible = false;

    [ObservableProperty]
    private bool _isPromptTextBoxReadOnly = false;

    [ObservableProperty]
    private bool _isErrorViewVisible = false;

    [ObservableProperty]
    private bool _isPromptGuidanceVisible = true;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCodespaceCommand))]
    private bool _areDependenciesPresent = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateCodespaceCommand))]
    private bool _canGenerateCodespace = false;

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

    private int _stepCounter;

    public QuickstartPlaygroundViewModel(
        ISetupFlowStringResource stringResource,
        IQuickStartProjectService quickStartProjectService,
        ILocalSettingsService localSettingsService,
        SetupFlowOrchestrator orchestrator,
        IExperimentationService experimentationService)
        : base(stringResource, orchestrator)
    {
        IsStepPage = false;

        _quickStartProjectService = quickStartProjectService;
        _localSettingsService = localSettingsService;

        ActivityId = orchestrator.ActivityId;

        _experimentationService = experimentationService;

        // Placeholder launch text while button is disabled.
        LaunchButtonText = StringResource.GetLocalized(StringResourceKey.QuickstartPlaygroundLaunchButton, string.Empty);
    }

    [RelayCommand(CanExecute = nameof(EnableProjectButtons))]
    public Task SaveProject()
    {
        return Task.Run(async () =>
        {
            TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundSaveProjectClicked", relatedActivityId: ActivityId);

            // TODO: Replace with WindowSaveFileDialog
            var folderPicker = new FolderPicker();
            var hWnd = Application.Current.GetService<Window>().GetWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);
            folderPicker.FileTypeFilter.Add("*");

            var location = await folderPicker.PickSingleFolderAsync();
            if (!string.IsNullOrWhiteSpace(location?.Path))
            {
                CopyDirectory(_outputFolderForCurrentPrompt, location.Path);
                TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundSaveProjectCompleted", relatedActivityId: ActivityId);
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
            TelemetryFactory.Get<ITelemetry>().LogException("QuickstartPlaygroundDeleteDirectoryContents", ex, ActivityId);
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

        // TODO: Eventually the Dev Home UI will need to be generalized to support
        // multiple reference samples. For now, it displays the first one returned.
        ReferenceSampleUri = _quickStartProject.ReferenceSamples.FirstOrDefault();
        if (ReferenceSampleUri == null)
        {
            _log.Error("Failed to retrieve a valid reference sample URI when setting up FileView for project");
        }
        else
        {
            _log.Information($"Using {ReferenceSampleUri.AbsoluteUri} as reference sample");
        }

        // Prepare the progress bar for the next run (this also ensures that the user doesn't see
        // stale data from the previous run when they click generate the next time).
        ProgressValue = 0;
        ProgressMessage = string.Empty;

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

    [RelayCommand]
    public async Task SubmitChat()
    {
        var message = CustomPrompt;

        if (message != null && message != string.Empty)
        {
            ShowExamplePrompts = false;
            ChatMessages.Add(new ChatStyleMessage
            {
                Name = message,
                Type = ChatStyleMessage.ChatMessageItemType.Request,
            });

            await GenerateChatStyleCompetions(message);

            CanGenerateCodespace = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerateCodespace))]
    public async Task GenerateCodespace()
    {
        if (CustomPrompt != null && CustomPrompt != string.Empty && ActiveQuickstartSelection != null)
        {
            try
            {
                // Clear chat
                ChatMessages.Clear();

                var userPrompt = CustomPrompt;

                // TODO: Replace this (and throughout) with proper diagnostics / logging
                ArgumentNullException.ThrowIfNullOrEmpty(userPrompt);
                ArgumentNullException.ThrowIfNull(ActiveQuickstartSelection);

                TelemetryFactory.Get<ITelemetry>().Log("QuickstartPlaygroundGenerateButtonClicked", LogLevel.Critical, new GenerateButtonClicked(userPrompt), ActivityId);

                // Ensure file view isn't visible (in the case where the user has previously run a Generate command)
                IsFileViewVisible = false;
                IsErrorViewVisible = false;

                // Ensure that the launch buttons are in their default state. This is important for scenarios where
                // the extension has more than one project host and the user is doing multiple generate attempts in sequence.
                // It makes sure that the code in the code-behind for populating the dropdown button gets re-run to pick up
                // the new project host objects.
                IsLaunchButtonVisible = true;
                IsLaunchDropDownVisible = false;

                // Without this, when the user generates two projects in sequence, the text box
                // will contain any text from last-opened file in the previous project (which is
                // confusing as this project may not have anything to do with the current one). This
                // ensures that the file view is back to a known, clean state.
                GeneratedFileContent = string.Empty;

                // Temporarily turn off the provider combobox and ensure user cannot edit the prompt for the moment
                EnableQuickstartProjectCombobox = false;
                IsPromptTextBoxReadOnly = true;
                EnableProjectButtons = false;
                SetupChat();

                IProgress<QuickStartProjectProgress> progress = new Progress<QuickStartProjectProgress>(UpdateProgress);

                var outputFolder = await GetOutputFolder();
                _outputFolderForCurrentPrompt = outputFolder.Path;

                _quickStartProjectGenerationOperation = ActiveQuickstartSelection.CreateProjectGenerationOperation(userPrompt, outputFolder, ActivityId);
                _quickStartProject = await _quickStartProjectGenerationOperation.GenerateAsync().AsTask(progress);
                _quickStartProjectGenerationOperation = null!;
                if (_quickStartProject.Result.Status == ProviderOperationStatus.Success)
                {
                    SetUpFileView();
                    SetupLaunchButton();
                    EnableProjectButtons = true;
                    TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundGenerateSuccceded", relatedActivityId: ActivityId);
                }
                else
                {
                    IsErrorViewVisible = true;
                    ErrorMessage = StringResource.GetLocalized("QuickstartPlaygroundGenerationFailedDetails", _quickStartProject.Result.DisplayMessage);
                    TelemetryFactory.Get<ITelemetry>().Log(
                        "QuickstartPlaygroundGenerateFailed",
                        LogLevel.Critical,
                        new ProjectGenerationErrorInfo(_quickStartProject.Result.DisplayMessage, _quickStartProject.Result.ExtendedError, _quickStartProject.Result.DiagnosticText),
                        ActivityId);
                }
            }
            finally
            {
                // Re-enable the provider combobox and prompt textbox
                EnableQuickstartProjectCombobox = true;
                IsPromptTextBoxReadOnly = false;
                CanGenerateCodespace = false;
                IsPromptValid = false;
            }
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
        TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundLaunchProjectClicked", relatedActivityId: ActivityId);
        var projectHostToLaunch = projectHost ?? QuickStartProjectHosts[0];
        await Task.Run(projectHostToLaunch.Launch);
    }

    private bool EnableReferenceSample()
    {
        return ReferenceSampleUri != null;
    }

    [RelayCommand(CanExecute = nameof(EnableReferenceSample))]
    private async Task OpenReferenceSample()
    {
        if (ReferenceSampleUri != null)
        {
            _log.Information("Launching reference sample for generated project");
            await Launcher.LaunchUriAsync(ReferenceSampleUri);
        }
        else
        {
            _log.Error("User has requested to see reference sample but the URI from the extension is null.");
        }
    }

    public void ProvideFeedback(bool isPositive, string feedback)
    {
        TelemetryFactory.Get<ITelemetry>().Log("QuickstartPlaygroundFeedbackSubmitted", LogLevel.Critical, new FeedbackSubmitted(isPositive, feedback), ActivityId);
        _quickStartProject?.FeedbackHandler?.ProvideFeedback(isPositive, feedback);
    }

    [RelayCommand]
    public void CopyExamplePrompt(string selectedPrompt)
    {
        CustomPrompt = selectedPrompt;
    }

    public async Task GenerateChatStyleCompetions(string message)
    {
        _stepCounter++;
        if (ActiveQuickstartSelection != null)
        {
            _quickStartChatStyleGeneration = ActiveQuickstartSelection.CreateChatStyleGenerationOperation(message);
            var result = await _quickStartChatStyleGeneration.GenerateChatStyleResponse();
            ChatMessages.Add(new ChatStyleMessage
            {
                Name = result.ChatResponse.ToString(),
                Type = ChatStyleMessage.ChatMessageItemType.Response,
            });

            if (result.ChatResponse.ToString().Contains("great prompt"))
            {
                IsPromptValid = true;
                CustomPrompt = message;
            }
            else
            {
                IsPromptValid = false;
            }
        }
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
            IsPromptValid = false;
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
            ReferenceSampleUri = null;
            ChatMessages.Clear();
            _stepCounter = 0;

            SetupChat();

            // Update our setting to indicate the user preference
            _localSettingsService.SaveSettingAsync("QuickstartPlaygroundSelectedProvider", ActiveQuickstartSelection.DisplayName);

            _log.Information("Completed setup work for extension selection");
        }
        else
        {
            _log.Information("Reset extension selection");

            ShowExamplePrompts = false;
            ShowPrivacyAndTermsLink = false;
            IsLaunchButtonVisible = false;
            ConfigureForProviderSelection();
        }
    }

    private void SetupChat()
    {
        ChatMessages.Clear();

        ChatMessages.Add(new ChatStyleMessage
        {
            Name = "Hello! What kind of project would you like to create? Try a sample prompt or write one of your own.",
            Type = ChatStyleMessage.ChatMessageItemType.Response,
        });

        ChatMessages.Add(new ChatStyleMessage
        {
            Name = "If you would like to help us guide you to a good prompt, hit the Enter/Return key on your keyboard or press Submit after typing in your prompt. Otherwise, simply click on Generate to ignore prompt guidance.",
            Type = ChatStyleMessage.ChatMessageItemType.Response,
        });
    }

    [RelayCommand]
    private void OnLoaded()
    {
        var isEnabled = _experimentationService.IsFeatureEnabled(MainPageViewModel.QuickstartPlaygroundFlowFeatureName);
        if (!isEnabled)
        {
            var setupFlow = Application.Current.GetService<SetupFlowViewModel>();
            setupFlow.ResetToMainPage();
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

    private ObservableCollection<ExplorerItem>? _children;

    public ObservableCollection<ExplorerItem> Children
    {
        get
        {
            _children ??= new ObservableCollection<ExplorerItem>();
            return _children;
        }
        set => _children = value;
    }

    private bool _isExpanded;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
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

public class ChatStyleMessage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public enum ChatMessageItemType
    {
        Request,
        Response,
    }

    public string? Name
    {
        get; set;
    }

    public ChatMessageItemType Type
    {
        get; set;
    }

    public string? FullPath
    {
        get; set;
    }

    private ObservableCollection<ChatMessageItemType>? _children;

    public ObservableCollection<ChatMessageItemType> Children
    {
        get
        {
            _children ??= new ObservableCollection<ChatMessageItemType>();
            return _children;
        }
        set => _children = value;
    }

    private bool _isExpanded;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                NotifyPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ChatRequestMessageTemplate
    {
        get; set;
    }

    public DataTemplate? ChatResponseMessageTemplate
    {
        get; set;
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        var explorerItem = (ChatStyleMessage)item;
        return explorerItem.Type == ChatStyleMessage.ChatMessageItemType.Request ? ChatRequestMessageTemplate : ChatResponseMessageTemplate;
    }
}
