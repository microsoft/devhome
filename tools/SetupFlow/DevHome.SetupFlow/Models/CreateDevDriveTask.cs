// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.ElevatedComponent;
using DevHome.SetupFlow.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;

namespace DevHome.SetupFlow.Models;

internal class CreateDevDriveTask : ISetupTask
{
    private readonly TaskMessages _taskMessages;
    private readonly ActionCenterMessages _actionCenterMessages = new ();
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IHost _host;
    private readonly ILogger _logger;

    public bool RequiresAdmin => true;

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled => false;

    public IDevDrive DevDrive
    {
        get; set;
    }

    public CreateDevDriveTask(IDevDrive devDrive, IHost host, ILogger logger, ISetupFlowStringResource stringResource)
    {
        DevDrive = devDrive;
        _stringResource = stringResource;
        _taskMessages = new TaskMessages
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.DevDriveCreating),
            Finished = _stringResource.GetLocalized(StringResourceKey.DevDriveCreated),
            Error = _stringResource.GetLocalized(StringResourceKey.DevDriveUnableToCreateError),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.DevDriveRestart),
        };
        _host = host;
        _logger = logger;
    }

    public ActionCenterMessages GetErrorMessages() => _actionCenterMessages;

    public TaskMessages GetLoadingMessages() => _taskMessages;

    public ActionCenterMessages GetRebootMessage() => new ();

    /// <summary>
    /// Not used, as Dev Drive creation requires elevation
    /// </summary>
    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(() =>
        {
            return TaskFinishedState.Failure;
        }).AsAsyncOperation();
    }

    /// <summary>
    /// Gets the localized system error message from the HResult passed into
    /// the function.
    /// </summary>
    /// <param name="errorCode">Error code that comes from the CreateDevDrive method</param>
    /// <returns>
    /// Localized string error message from hresult if exists on the system else just the error code in Hexidecimal format
    /// </returns>
    public string GetLocalizedErrorMsg(int errorCode)
    {
        unsafe
        {
            PWSTR formattedMessage;
            var msgLength = PInvoke.FormatMessage(
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_FROM_SYSTEM |
                FORMAT_MESSAGE_OPTIONS.FORMAT_MESSAGE_IGNORE_INSERTS,
                null,
                unchecked((uint)errorCode),
                0,
                (PWSTR)(void*)&formattedMessage,
                0,
                null);
            try
            {
                if (msgLength == 0)
                {
                    // if formatting the error code into a message fails, then log this and just return the error code.
                    Log.Logger?.ReportError(nameof(CreateDevDriveTask), $"Failed to format error code.  0x{errorCode:X}");
                    return $"(0x{errorCode:X})";
                }

                return new string(formattedMessage.Value, 0, (int)msgLength) + $" (0x{errorCode:X})";
            }
            finally
            {
                PInvoke.LocalFree((IntPtr)formattedMessage.Value);
            }
        }
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory)
    {
        return Task.Run(() =>
        {
            try
            {
                // Create the location if it doesn't exist. Do this before validation.
                if (!Directory.Exists(DevDrive.DriveLocation))
                {
                    Directory.CreateDirectory(DevDrive.DriveLocation);
                }

                var manager = _host.GetService<IDevDriveManager>();
                var validation = manager.GetDevDriveValidationResults(DevDrive);
                manager.RemoveAllDevDrives();

                if (!validation.Contains(DevDriveValidationResult.Successful))
                {
                    var localizedMsg = _stringResource.GetLocalized("DevDrive" + validation.First().ToString());
                    _actionCenterMessages.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.DevDriveErrorWithReason, localizedMsg);
                    return TaskFinishedState.Failure;
                }

                var storageOperator = elevatedComponentFactory.CreateDevDriveStorageOperator();
                var virtDiskPath = Path.Combine(DevDrive.DriveLocation, DevDrive.DriveLabel + ".vhdx");
                var result = storageOperator.CreateDevDrive(virtDiskPath, DevDrive.DriveSizeInBytes, DevDrive.DriveLetter, DevDrive.DriveLabel);
                if (result != 0)
                {
                    _actionCenterMessages.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.DevDriveErrorWithReason, GetLocalizedErrorMsg(result));
                    Log.Logger?.ReportError(nameof(CreateDevDriveTask), $"Failed to create Dev Drive, Error code. 0x{result:X}");
                    return TaskFinishedState.Failure;
                }

                return TaskFinishedState.Success;
            }
            catch (Exception ex)
            {
                Log.Logger?.ReportError(nameof(CreateDevDriveTask), $"Failed to create Dev Drive. Due to Exception ErrorCode: 0x{ex.HResult:X}, Msg: {ex.Message}");
                _actionCenterMessages.PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.DevDriveErrorWithReason, GetLocalizedErrorMsg(ex.HResult));
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }
}
