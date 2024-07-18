// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Antlr4.Runtime.Misc;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

internal class SSHWalletWidget : CoreWidget
{
    protected static readonly string DefaultConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.ssh\\config";

    private static readonly Regex HostRegex = new (@"^Host\s+(\S*)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private FileSystemWatcher? FileWatcher { get; set; }

    protected static readonly new string Name = nameof(SSHWalletWidget);

    protected string ConfigFile
    {
        get => State();

        set => SetState(value);
    }

    public SSHWalletWidget()
    {
    }

    ~SSHWalletWidget()
    {
        // Ensures widget is in the proper view when a user returns
        UpdateWidget();
        FileWatcher?.Dispose();
    }

    public override void LoadContentData()
    {
        // If ConfigFile is not set, do nothing.
        // Widget will remain in configuring state, waiting for config file path input.
        if (string.IsNullOrWhiteSpace(ConfigFile))
        {
            ContentData = new JsonObject { { "configuring", true } }.ToJsonString();
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting SSH Hosts");

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
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
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
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.Connect:
                HandleConnect(actionInvokedArgs);
                break;

            case WidgetAction.CheckPath:
                HandleCheckPath(actionInvokedArgs);
                break;

            case WidgetAction.PatternConnect:
                HandleConnect(actionInvokedArgs, true);
                break;

            case WidgetAction.Unknown:
                Log.Logger()?.ReportError(Name, ShortId, $"Unknown verb: {actionInvokedArgs.Verb}");
                break;
        }
    }

    private void HandleConnect(WidgetActionInvokedArgs args, bool matchingPattern = false)
    {
        var data = args.Data;

        if (matchingPattern)
        {
            var jsonObject = JsonSerializer.Deserialize<JsonNode>(data);
            var patternHostNode = jsonObject != null ? jsonObject["PatternHost"] : null;
            Log.Logger()?.ReportDebug(Name, ShortId, $"help data: {patternHostNode}");
            if (patternHostNode != null)
            {
                data = patternHostNode.ToString();
            }
        }

        if (data.Contains('*') || data.Contains('?'))
        {
            Page = WidgetPageState.Pattern;
            UpdateWidget(data);
            return;
        }

        Process cmd = new Process();

        var info = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k \"ssh {data}\"",
            UseShellExecute = true,
        };

        cmd.StartInfo = info;

        cmd.Start();

        // If we get here we have tried to connect so reset to the ssh host list
        Page = WidgetPageState.Content;
        UpdateWidget();
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
    }

    private MatchCollection? GetHostEntries()
    {
        FileStreamOptions options = new FileStreamOptions();
        options.Access = FileAccess.Read;

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
        if (hostEntries == null)
        {
            return 0;
        }

        return hostEntries.Count;
    }

    private void SetupFileWatcher()
    {
        var configFileDir = Path.GetDirectoryName(ConfigFile);
        var configFileName = Path.GetFileName(ConfigFile);

        if (configFileDir != null && configFileName != null )
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

    private JsonObject FillConfigurationData(bool hasConfiguration, string configFile, int numOfEntries = 0, bool configuring = true, string errorMessage = "")
    {
        var configurationData = new JsonObject();

        var sshConfigData = new JsonObject
            {
                { "configFile", configFile },
                { "defaultConfigFile", DefaultConfigFile },
                { "numOfEntries", numOfEntries.ToString(CultureInfo.InvariantCulture) },
            };

        configurationData.Add("configuring", configuring);
        configurationData.Add("hasConfiguration", hasConfiguration);
        configurationData.Add("configuration", sshConfigData);
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

                    configurationData = FillConfigurationData(true, ConfigFile, numberOfEntries, false);
                }
                else
                {
                    configurationData = FillConfigurationData(false, data, 0, true, Resources.GetResource(@"SSH_Widget_Template/ConfigFileNotFound", Logger()));
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError(Name, ShortId, $"Failed getting configuration information for input config file path: {data}", ex);

                configurationData = FillConfigurationData(false, data, 0, true, Resources.GetResource(@"SSH_Widget_Template/ErrorProcessingConfigFile", Logger()));

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
        WidgetUpdateRequestOptions updateOptions = new (Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
            CustomState = ConfigFile,
        };

        Log.Logger()?.ReportDebug(Name, ShortId, $"Updating widget for {Page}");
        WidgetManager.GetDefault().UpdateWidget(updateOptions);
    }

    public void UpdateWidget(string patternHost)
    {
        // If patternHost is a JSON string, remove the leading and trailing quotes
        if (patternHost.StartsWith("\"", StringComparison.Ordinal) && patternHost.EndsWith("\"", StringComparison.Ordinal))
        {
            patternHost = patternHost.Trim('"');
        }

        WidgetUpdateRequestOptions updateOptions = new (Id)
        {
            Data = new JsonObject { { "patternHost", patternHost } }.ToJsonString(),
            Template = GetTemplateForPage(Page),
            CustomState = ConfigFile,
        };

        Log.Logger()?.ReportDebug(Name, ShortId, $"Updating widget for {Page}");
        WidgetManager.GetDefault().UpdateWidget(updateOptions);
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Configure => @"Widgets\Templates\SSHWalletConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\SSHWalletTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\LoadingTemplate.json",
            WidgetPageState.Pattern => @"Widgets\Templates\SSHWalletPatternMatching.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Configure => GetConfiguration(ConfigFile),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => new JsonObject { { "configuring", true } }.ToJsonString(),

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

internal class DataPayload
{
    public string? ConfigFile
    {
        get; set;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DataPayload))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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

internal class SSHWalletWidget : CoreWidget
{
    protected static readonly string DefaultConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.ssh\\config";

    private static readonly Regex HostRegex = new (@"^Host\s+(\S*)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private FileSystemWatcher? FileWatcher { get; set; }

    protected static readonly new string Name = nameof(SSHWalletWidget);

    protected string ConfigFile
    {
        get => State();

        set => SetState(value);
    }

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
            ContentData = new JsonObject { { "configuring", true } }.ToJsonString();
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting SSH Hosts");

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
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
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
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        switch (verb)
        {
            case WidgetAction.Connect:
                HandleConnect(actionInvokedArgs);
                break;

            case WidgetAction.CheckPath:
                HandleCheckPath(actionInvokedArgs);
                break;

            case WidgetAction.Unknown:
                Log.Logger()?.ReportError(Name, ShortId, $"Unknown verb: {actionInvokedArgs.Verb}");
                break;
        }
    }

    private void HandleConnect(WidgetActionInvokedArgs args)
    {
        var data = args.Data;

        Process cmd = new Process();

        var info = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k \"ssh {data}\"",
            UseShellExecute = true,
        };

        cmd.StartInfo = info;

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
    }

    private MatchCollection? GetHostEntries()
    {
        FileStreamOptions options = new FileStreamOptions();
        options.Access = FileAccess.Read;

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
        if (hostEntries == null)
        {
            return 0;
        }

        return hostEntries.Count;
    }

    private void SetupFileWatcher()
    {
        var configFileDir = Path.GetDirectoryName(ConfigFile);
        var configFileName = Path.GetFileName(ConfigFile);

        if (configFileDir != null && configFileName != null )
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

    private JsonObject FillConfigurationData(bool hasConfiguration, string configFile, int numOfEntries = 0, bool configuring = true, string errorMessage = "")
    {
        var configurationData = new JsonObject();

        var currentOrDefaultConfigFile = string.IsNullOrEmpty(configFile) ? DefaultConfigFile : configFile;
        var sshConfigData = new JsonObject
            {
                { "configFile", configFile },
                { "currentOrDefaultConfigFile", currentOrDefaultConfigFile },
                { "numOfEntries", numOfEntries.ToString(CultureInfo.InvariantCulture) },
            };

        configurationData.Add("configuring", configuring);
        configurationData.Add("hasConfiguration", hasConfiguration);
        configurationData.Add("configuration", sshConfigData);
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

                    configurationData = FillConfigurationData(true, ConfigFile, numberOfEntries, false);
                }
                else
                {
                    configurationData = FillConfigurationData(false, data, 0, true, Resources.GetResource(@"SSH_Widget_Template/ConfigFileNotFound", Logger()));
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError(Name, ShortId, $"Failed getting configuration information for input config file path: {data}", ex);

                configurationData = FillConfigurationData(false, data, 0, true, Resources.GetResource(@"SSH_Widget_Template/ErrorProcessingConfigFile", Logger()));

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
        WidgetUpdateRequestOptions updateOptions = new (Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
            CustomState = ConfigFile,
        };

        Log.Logger()?.ReportDebug(Name, ShortId, $"Updating widget for {Page}");
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
            WidgetPageState.Loading => new JsonObject { { "configuring", true } }.ToJsonString(),

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

internal class DataPayload
{
    public string? ConfigFile
    {
        get; set;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DataPayload))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
