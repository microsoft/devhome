// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Exceptions;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;
using Windows.Foundation;

namespace DevHome.SetupFlow.AppManagement.Models;

public class InstallPackageTask : ISetupTask
{
    private readonly ILogger _logger;
    private readonly IWindowsPackageManager _wpm;
    private readonly WinGetPackage _package;
    private readonly ISetupFlowStringResource _stringResource;

    // TODO Use WinGet COM API to get this information when integrating with
    // the elevated process changes
    public bool RequiresAdmin => false;

    // As we don't have this information available for each package in the WinGet COM API,
    // simply assume that any package installation may need a reboot.
    public bool RequiresReboot => true;

    public LoadingMessages GetLoadingMessages()
    {
        return new LoadingMessages
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.InstallingPackage, _package.Name),
            Error = _stringResource.GetLocalized(StringResourceKey.InstallPackageError, _package.Name),
            Finished = _stringResource.GetLocalized(StringResourceKey.InstallingPackage, _package.Name),
        };
    }

    public InstallPackageTask(
        ILogger logger,
        IWindowsPackageManager wpm,
        ISetupFlowStringResource stringResource,
        WinGetPackage package)
    {
        _logger = logger;
        _wpm = wpm;
        _stringResource = stringResource;
        _package = package;
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            try
            {
                await _wpm.InstallPackageAsync(_package);
                return TaskFinishedState.Success;
            }
            catch (InstallPackageException e)
            {
                _logger.LogError(nameof(InstallPackageTask), LogLevel.Local, $"Failed to install package with status: {e.Status}");
                return TaskFinishedState.Failure;
            }
            catch (Exception e)
            {
                _logger.LogError(nameof(InstallPackageTask), LogLevel.Local, $"Exception thrown while installing package: {e.Message}");
                _logger.LogError(nameof(InstallPackageTask), LogLevel.Local, e.Message);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }
}
