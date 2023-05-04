// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;

namespace CoreWidgetProvider.Widgets;

public class SSHWalletWidget : WidgetImpl
{
    protected static readonly string EmptyJson = new JsonObject().ToJsonString();
    protected static readonly string DefaultConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.ssh\\config";

    private static readonly Regex HostRegex = new (@"^Host\s+(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private FileSystemWatcher? FileWatcher { get; set; }

    protected string ContentData { get; set; } = EmptyJson;

    protected static readonly string Name = nameof(SSHWalletWidget);

    protected WidgetActivityState ActivityState { get; set; } = WidgetActivityState.Unknown;

    protected WidgetDataState DataState { get; set; } = WidgetDataState.Unknown;

    protected WidgetPageState Page { get; set; } = WidgetPageState.Unknown;

    protected bool Enabled
    {
        get; set;
    }

    protected Dictionary<WidgetPageState, string> Template { get; set; } = new ();

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

    public virtual void LoadContentData()
    {
        if (string.IsNullOrWhiteSpace(ConfigFile))
        {
            ContentData = string.Empty;
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting SSH Hosts");

        try
        {
            var hostsData = new JsonObject();
            var hostsArray = new JsonArray();

            FileStreamOptions options = new FileStreamOptions();
            options.Access = FileAccess.Read;

            using var reader = new StreamReader(ConfigFile, options);

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (HostRegex.IsMatch(line))
                {
                    Match m = HostRegex.Match(line);
                    if (m.Success)
                    {
                        var host = m.Groups[1].Value;
                        var hostJson = new JsonObject
                        {
                            { "host", host },
                        };
                        ((IList<JsonNode?>)hostsArray).Add(hostJson);
                    }
                }
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

    public override void Activate(WidgetContext widgetContext)
    {
        Enabled = true;
        UpdateActivityState();
    }

    public override void Deactivate(string widgetId)
    {
        Enabled = false;
        UpdateActivityState();
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        Enabled = false;
        SetDeleted();
    }

    public override void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Enabled = contextChangedArgs.WidgetContext.IsActive;
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

    private WidgetAction GetWidgetActionForVerb(string verb)
    {
        try
        {
            return Enum.Parse<WidgetAction>(verb);
        }
        catch (Exception)
        {
            // Invalid verb.
            Log.Logger()?.ReportError($"Unknown WidgetAction verb: {verb}");
            return WidgetAction.Unknown;
        }
    }

    private int GetNumberOfHostEntries()
    {
        FileStreamOptions options = new FileStreamOptions();
        options.Access = FileAccess.Read;

        using var reader = new StreamReader(ConfigFile, options);

        var fileContent = reader.ReadToEnd();

        if (!string.IsNullOrEmpty(fileContent))
        {
            return HostRegex.Matches(fileContent).Count;
        }

        return 0;
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

    private JsonObject FillConfigurationData(bool hasConfiguration, string configFile, int numOfEntries = 0, string errorMessage = "")
    {
        var configurationData = new JsonObject();

        var sshConfigData = new JsonObject
            {
                { "configFile", configFile },
                { "defaultConfigFile", DefaultConfigFile },
                { "numOfEntries", numOfEntries.ToString(CultureInfo.InvariantCulture) },
            };

        configurationData.Add("hasConfiguration", hasConfiguration);
        configurationData.Add("configuration", sshConfigData);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            configurationData.Add("errorMessage", errorMessage);
        }

        return configurationData;
    }

    public string GetConfiguration(string data)
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
                    configurationData = FillConfigurationData(false, data, 0, Resources.GetResource(@"SSH_Widget_Template/ConfigFileNotFound", Logger()));
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError(Name, ShortId, $"Failed getting configuration information for input config file path: {data}", ex);

                configurationData = FillConfigurationData(false, data, 0, Resources.GetResource(@"SSH_Widget_Template/ErrorProcessingConfigFile", Logger()));

                return configurationData.ToString();
            }
        }

        return configurationData.ToString();
    }

    public void UpdateActivityState()
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

    public void UpdateWidget()
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

    public virtual string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Configure => @"Widgets\Templates\SSHWalletConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\SSHWalletTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\LoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public virtual string GetData(WidgetPageState page)
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

    protected string GetTemplateForPage(WidgetPageState page)
    {
        if (Template.ContainsKey(page))
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Using cached template for {page}");
            return Template[page];
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, GetTemplatePath(page));
            var template = File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
            template = Resources.ReplaceIdentifers(template, Resources.GetWidgetResourceIdentifiers(), Log.Logger());
            Log.Logger()?.ReportDebug(Name, ShortId, $"Caching template for {page}");
            Template[page] = template;
            return template;
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error getting template.", e);
            return string.Empty;
        }
    }

    protected string GetCurrentState()
    {
        return $"State: {ActivityState}  Page: {Page}  Data: {DataState}  Config file: {ConfigFile}";
    }

    protected void LogCurrentState()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, GetCurrentState());
    }

    private void SetActive()
    {
        ActivityState = WidgetActivityState.Active;
        Page = WidgetPageState.Content;
        if (ContentData == EmptyJson)
        {
            LoadContentData();
        }

        LogCurrentState();
        UpdateWidget();
    }

    private void SetInactive()
    {
        ActivityState = WidgetActivityState.Inactive;

        // No need to update when we are inactive.
        LogCurrentState();
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

    private void SetDeleted()
    {
        SetState(string.Empty);
        ActivityState = WidgetActivityState.Unknown;
        LogCurrentState();
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
