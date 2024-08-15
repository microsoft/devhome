// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Models;
using DevHome.Common.Services;
using Serilog;

namespace DevHome.FileExplorerSourceControlIntegration.Services;

public class FileExplorerIntegrationUserSettings
{
    private readonly LocalSettingsService? _localSettingsService;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(FileExplorerIntegrationUserSettings));
    private const string DefaultApplicationDataFolder = "DevHome/ApplicationData";
    private const string DefaultLocalSettingsFile = "LocalSettings.json";

    public FileExplorerIntegrationUserSettings()
    {
        try
        {
            FileService fileService = new FileService();
            var options = new Microsoft.Extensions.Options.OptionsWrapper<LocalSettingsOptions>(
            new LocalSettingsOptions
            {
                ApplicationDataFolder = DefaultApplicationDataFolder,
                LocalSettingsFile = DefaultLocalSettingsFile,
            });

            // The LocalSettingsService has to be used and initialized from within the
            // File Explorer Source Control Integration COM Server
            _localSettingsService = new LocalSettingsService(fileService, options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get LocalSettingsService");
        }
    }

    public bool IsFileExplorerVersionControlEnabled()
    {
        if (_localSettingsService != null && _localSettingsService.HasSettingAsync("VersionControlIntegration").Result)
        {
            return _localSettingsService.ReadSettingAsync<bool>("VersionControlIntegration").Result;
        }

        // DisabledByDefault
        return false;
    }

    public bool ShowFileExplorerVersionControlColumnData()
    {
        if (_localSettingsService != null && _localSettingsService.HasSettingAsync("ShowVersionControlInformation").Result)
        {
            return _localSettingsService.ReadSettingAsync<bool>("ShowVersionControlInformation").Result;
        }

        // DisabledByDefault
        return false;
    }

    public bool ShowRepositoryStatus()
    {
        if (_localSettingsService != null && _localSettingsService.HasSettingAsync("ShowRepositoryStatus").Result)
        {
            return _localSettingsService.ReadSettingAsync<bool>("ShowRepositoryStatus").Result;
        }

        // DisabledByDefault
        return false;
    }
}
