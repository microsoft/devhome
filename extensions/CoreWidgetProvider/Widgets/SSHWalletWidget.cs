// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

internal sealed class SSHWalletWidget : CoreWidget
{
    private static readonly string DefaultConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.ssh\\config";

    private static readonly Regex HostRegex = new(@"^Host\s+(\S*)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private FileSystemWatcher? FileWatcher { get; set; }

    private string ConfigFile
    {
        get => State();

        set => SetState(value);
    }

    private string _savedContentData = string.Empty;
    private string _savedConfigFile = string.Empty;

    public SSHWalletWidget()
    {
    }

    ~SSHWalletWidget()
    {
        FileWatcher?.Dispose();
    }

    public override void LoadContentData()
    {
        // If ConfigFile is not set, do nothing.
        // Widget will remain in configuring state, waiting for config file path input.
        if (string.IsNullOrWhiteSpace(ConfigFile))
        {
            ContentData = EmptyJson;
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Debug("Getting SSH Hosts");

        // Read host entries from SSH config file and fill ContentData.
        // Widget will show host entries declared in ConfigFile.
        try
        {
            var hostsData = new JsonObject();
            var hostsArray = new JsonArray();

            var hostEntries = GetHostEntries();
            if (hostEntries != null)
            {
                hostEntries.ToList().ForEach(hostEntry =>
                {
                    var host = hostEntry.Groups[1].Value;
                    var hostJson = new JsonObject
                        {
                            { "host", host },
                            { "icon", IconLoader.GetIconAsBase64("connect_icon.png") },
                        };
                    ((IList<JsonNode?>)hostsArray).Add(hostJson);
                });
            }

            hostsData.Add("hosts", hostsArray);
            hostsData.Add("selected_config_file", ConfigFile);

            DataState = WidgetDataState.Okay;
            ContentData = hostsData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving data.");
            var content = new JsonObject
            {
                { "errorMessage", e.Message },
            };
            ContentData = content.ToJsonString();
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override void CreateWidget(WidgetContext widgetContext, string state)
    {
        Id = widgetContext.Id;
        Enabled = widgetContext.IsActive;
        ConfigFile = state;
        UpdateActivityState();
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Debug($"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.Connect:
                HandleConnect(actionInvokedArgs);
                break;

            case WidgetAction.CheckPath:
                HandleCheckPath(actionInvokedArgs);
                break;

            case WidgetAction.Unknown:
                Log.Error($"Unknown verb: {actionInvokedArgs.Verb}");
                break;

            case WidgetAction.Save:
                _savedContentData = string.Empty;
                _savedConfigFile = string.Empty;
                ContentData = EmptyJson;
                SetActive();
                break;

            case WidgetAction.Cancel:
                ConfigFile = _savedConfigFile;
                ContentData = _savedContentData;
                SetActive();
                break;

            case WidgetAction.ChooseFile:
                HandleCheckPath(actionInvokedArgs);
                break;
        }
    }

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        _savedContentData = ContentData;
        _savedConfigFile = ConfigFile;
        SetConfigure();
    }

    private void HandleConnect(WidgetActionInvokedArgs args)
    {
        var cmd = new Process();
        cmd.StartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k \"ssh {args.Data}\"",
            UseShellExecute = true,
        };

        cmd.Start();
    }

    private void HandleCheckPath(WidgetActionInvokedArgs args)
    {
        // Set loading page while we fetch data from config file.
        Page = WidgetPageState.Loading;
        UpdateWidget();

        // This is the action when the user clicks the submit button after entering a path while in
        // the Configure state.
        Page = WidgetPageState.Configure;
        var data = args.Data;
        var dataObject = JsonSerializer.Deserialize(data, SourceGenerationContext.Default.DataPayload);
        if (dataObject != null && dataObject.ConfigFile != null)
        {
            var updateRequestOptions = new WidgetUpdateRequestOptions(Id)
            {
                Data = GetConfiguration(dataObject.ConfigFile),
                CustomState = ConfigFile,
                Template = GetTemplateForPage(Page),
            };

            WidgetManager.GetDefault().UpdateWidget(updateRequestOptions);
        }

        // This is the action when the user uses the File Picker to select a file "filePath" in the Configure state.
        if (dataObject != null && dataObject.FilePath != null)
        {
            var updateRequestOptions = new WidgetUpdateRequestOptions(Id)
            {
                Data = GetConfiguration(dataObject.FilePath),
                CustomState = ConfigFile,
                Template = GetTemplateForPage(Page),
            };

            WidgetManager.GetDefault().UpdateWidget(updateRequestOptions);
        }
    }

    private MatchCollection? GetHostEntries()
    {
        var options = new FileStreamOptions()
        {
            Access = FileAccess.Read,
        };

        using var reader = new StreamReader(ConfigFile, options);

        var fileContent = reader.ReadToEnd();

        if (!string.IsNullOrEmpty(fileContent))
        {
            return HostRegex.Matches(fileContent);
        }

        return null;
    }

    private int GetNumberOfHostEntries()
    {
        var hostEntries = GetHostEntries();
        return (hostEntries != null) ? hostEntries.Count : 0;
    }

    private void SetupFileWatcher()
    {
        var configFileDir = Path.GetDirectoryName(ConfigFile);
        var configFileName = Path.GetFileName(ConfigFile);

        if (configFileDir != null && configFileName != null)
        {
            FileWatcher = new FileSystemWatcher(configFileDir, configFileName);

            FileWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            FileWatcher.Changed += OnConfigFileChanged;
            FileWatcher.Deleted += OnConfigFileDeleted;
            FileWatcher.Renamed += OnConfigFileRenamed;

            FileWatcher.IncludeSubdirectories = true;
            FileWatcher.EnableRaisingEvents = true;
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        LoadContentData();
        UpdateWidget();
    }

    private void OnConfigFileDeleted(object sender, FileSystemEventArgs e)
    {
        SetConfigure();
    }

    private void OnConfigFileRenamed(object sender, FileSystemEventArgs e)
    {
        ConfigFile = e.FullPath;
        LoadContentData();
        UpdateWidget();
    }

    private JsonObject FillConfigurationData(bool hasConfiguration, string configFile, int numOfEntries = 0, string errorMessage = "")
    {
        var configurationData = new JsonObject();

        // Determine what config file to suggest in configuration form.
        // 1. If there is a currently selected configFile, show that.
        // 2. Else, check if there is a _savedConfigFile. If so, the user
        //    is in the customize flow and we should show the _savedConfigFile.
        // 3. Else, show the DefaultConfigFile.
        var suggestedConfigFile = string.IsNullOrEmpty(configFile) ? _savedConfigFile : configFile;
        suggestedConfigFile = string.IsNullOrEmpty(suggestedConfigFile) ? DefaultConfigFile : suggestedConfigFile;

        var sshConfigData = new JsonObject
            {
                { "configFile", configFile },
                { "currentOrDefaultConfigFile", suggestedConfigFile },
                { "numOfEntries", numOfEntries.ToString(CultureInfo.InvariantCulture) },
            };

        configurationData.Add("hasConfiguration", hasConfiguration);
        configurationData.Add("configuration", sshConfigData);
        configurationData.Add("savedConfigFile", _savedConfigFile);
        configurationData.Add("submitIcon", IconLoader.GetIconAsBase64("arrow.png"));

        if (!string.IsNullOrEmpty(errorMessage))
        {
            configurationData.Add("errorMessage", errorMessage);
        }

        return configurationData;
    }

    public override string GetConfiguration(string data)
    {
        JsonObject? configurationData;

        if (string.IsNullOrWhiteSpace(data))
        {
            configurationData = FillConfigurationData(false, string.Empty);
        }
        else
        {
            try
            {
                if (File.Exists(data))
                {
                    ConfigFile = data;
                    SetupFileWatcher();

                    var numberOfEntries = GetNumberOfHostEntries();

                    configurationData = FillConfigurationData(true, ConfigFile, numberOfEntries);
                }
                else
                {
                    configurationData = FillConfigurationData(false, data, 0, Resources.GetResource(@"SSH_Widget_Template/ConfigFileNotFound", Log));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed getting configuration information for input config file path: {data}");

                configurationData = FillConfigurationData(false, data, 0, Resources.GetResource(@"SSH_Widget_Template/ErrorProcessingConfigFile", Log));

                return configurationData.ToString();
            }
        }

        return configurationData.ToString();
    }

    public override void UpdateActivityState()
    {
        if (string.IsNullOrEmpty(ConfigFile))
        {
            SetConfigure();
            return;
        }

        if (Enabled)
        {
            SetActive();
            return;
        }

        SetInactive();
    }

    public override void UpdateWidget()
    {
        WidgetUpdateRequestOptions updateOptions = new(Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
            CustomState = ConfigFile,
        };

        Log.Debug($"Updating widget for {Page}");
        WidgetManager.GetDefault().UpdateWidget(updateOptions);
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Configure => @"Widgets\Templates\SSHWalletConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\SSHWalletTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\LoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Configure => GetConfiguration(ConfigFile),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,

            // In case of unknown state default to empty data
            _ => EmptyJson,
        };
    }

    protected override void SetActive()
    {
        ActivityState = WidgetActivityState.Active;
        Page = WidgetPageState.Content;
        if (ContentData == EmptyJson)
        {
            LoadContentData();
        }

        if (!string.IsNullOrEmpty(ConfigFile) && FileWatcher == null)
        {
            SetupFileWatcher();
        }

        LogCurrentState();
        UpdateWidget();
    }

    private void SetConfigure()
    {
        FileWatcher?.Dispose();
        ActivityState = WidgetActivityState.Configure;
        ConfigFile = string.Empty;
        Page = WidgetPageState.Configure;
        LogCurrentState();
        UpdateWidget();
    }
}

internal sealed class DataPayload
{
    public string? ConfigFile
    {
        get; set;
    }

    [JsonPropertyName("filePath")]
    public string? FilePath
    {
        get; set;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DataPayload))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{
}
