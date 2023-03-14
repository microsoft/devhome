// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerShell;
using Microsoft.Win32.SafeHandles;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.Vhd;
using Windows.Win32.System.Ioctl;

namespace DevHome.SetupFlow.DevDrive.Utilities;

/// <summary>
/// Class that will perform storage operations related to Dev Drives.
/// </summary>
public class DevDriveStorageOperator : IDevDriveStorageOperator
{
    private readonly PowerShell _powerShell;
    private readonly Runspace _runSpace;

    // Can be found publicly in virtdisk.h.
    private readonly Guid _microsoftVirtualVendorGuid = new (0xec984aec, 0xa0f9, 0x47e9, 0x90, 0x1f, 0x71, 0x41, 0x5a, 0x66, 0x34, 0x5b );

    // VIRTUAL_STORAGE_TYPE_DEVICE_VHDX enum
    private readonly uint _vhdxStorageTypeEnum = 3;
    private readonly VIRTUAL_STORAGE_TYPE _storageType;
    private readonly VIRTUAL_DISK_ACCESS_MASK _accessMask = VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_NONE;
    private const int MaxPhysicalPath = 260;

    // IOCTL_STORAGE_GET_DEVICE_NUMBER for getting device number
    private const int IoctlStorageGetDeviceNumber = 0x2D1080;

    public DevDriveStorageOperator()
    {
        _storageType = new VIRTUAL_STORAGE_TYPE
        {
            VendorId = _microsoftVirtualVendorGuid,
            DeviceId = _vhdxStorageTypeEnum,
        };

        // Create a default initial session state and set the execution policy.
        InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        initialSessionState.ExecutionPolicy = ExecutionPolicy.Unrestricted;

        // Create a runspace and open it.
        _runSpace = RunspaceFactory.CreateRunspace(initialSessionState);
        _runSpace.Open();
        _powerShell = PowerShell.Create();
        _powerShell.Runspace = _runSpace;
    }

    public int CreateAndAttachVhd(string path, ulong size)
    {
        var vhdParams = new CREATE_VIRTUAL_DISK_PARAMETERS
        {
            Version = CREATE_VIRTUAL_DISK_VERSION.CREATE_VIRTUAL_DISK_VERSION_2,
        };
        vhdParams.Anonymous.Version2.MaximumSize = 20ul * (1024ul * 1024ul * 1024ul);
        SafeFileHandle tempHandle;
        var securityDescriptor = new PSECURITY_DESCRIPTOR { };
        var result = PInvoke.CreateVirtualDisk(
            _storageType,
            path,
            _accessMask,
            securityDescriptor,
            CREATE_VIRTUAL_DISK_FLAG.CREATE_VIRTUAL_DISK_FLAG_NONE,
            0,
            vhdParams,
            null,
            out tempHandle);
        if (result != 0)
        {
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        var attachVhdFlags = ATTACH_VIRTUAL_DISK_FLAG.ATTACH_VIRTUAL_DISK_FLAG_NONE | ATTACH_VIRTUAL_DISK_FLAG.ATTACH_VIRTUAL_DISK_FLAG_PERMANENT_LIFETIME;
        result = PInvoke.AttachVirtualDisk(
            tempHandle,
            securityDescriptor,
            attachVhdFlags,
            0,
            null,
            null);

        tempHandle.Close();
        return PInvoke.HRESULT_FROM_WIN32(result);
    }

    public int GetDiskNumber(string path, out uint diskNumber)
    {
        diskNumber = 0;
        var vhdParams = new OPEN_VIRTUAL_DISK_PARAMETERS
        {
            Version = OPEN_VIRTUAL_DISK_VERSION.OPEN_VIRTUAL_DISK_VERSION_2,
        };
        vhdParams.Anonymous.Version2.GetInfoOnly = true;
        SafeFileHandle tempHandle;
        var result = PInvoke.OpenVirtualDisk(
            _storageType,
            path,
            _accessMask,
            OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE,
            vhdParams,
            out tempHandle);
        if (result != 0)
        {
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        var physicalPath = new char[MaxPhysicalPath];
        uint physicalPathLen = MaxPhysicalPath * sizeof(char);
        unsafe
        {
            fixed (char* pathPtr = physicalPath)
            {
                result = PInvoke.GetVirtualDiskPhysicalPath(
                    tempHandle,
                    ref physicalPathLen,
                    pathPtr);
            }
        }

        if (result != 0)
        {
            return PInvoke.HRESULT_FROM_WIN32(result);
        }

        var accessFlags = new FILE_ACCESS_FLAGS { };
        var fileFlagsAndAttributes = new FILE_FLAGS_AND_ATTRIBUTES { };
        SafeFileHandle diskHandle = PInvoke.CreateFile(
            physicalPath.ToString(),
            accessFlags,
            FILE_SHARE_MODE.FILE_SHARE_READ,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            fileFlagsAndAttributes,
            null);
        if (diskHandle is null)
        {
            return Marshal.GetLastWin32Error();
        }

        uint unusedBytesReturned = 0;
        var deviceNumber = new STORAGE_DEVICE_NUMBER { };
        unsafe
        {
            var wasSuccessful = PInvoke.DeviceIoControl(
                diskHandle,
                IoctlStorageGetDeviceNumber,
                null,
                0,
                &deviceNumber,
                (uint)sizeof(STORAGE_DEVICE_NUMBER),
                &unusedBytesReturned,
                null);
        }

        tempHandle.Close();
        diskHandle.Close();
        diskNumber = deviceNumber.DeviceNumber;
        return PInvoke.HRESULT_FROM_WIN32(result);
    }

    public async Task<int> InitializeDisk(uint diskNumber)
    {
        try
        {
            _powerShell.Commands.Clear();
            _powerShell.Streams.Error.Clear();
            _powerShell.AddCommand("initialize-Disk").AddParameter("Number", diskNumber);
            var results = await _powerShell.InvokeAsync();
            var operationStatus = _powerShell.Streams.Error.Last().Exception.HResult;
            if (!results.Any())
            {
                // log error.
            }

            return operationStatus;
        }
        catch (Exception)
        {
            // log error and throw
            throw;
        }
    }

    public async Task<int> CreatePartition(uint diskNumber, char driveLetter)
    {
        try
        {
            _powerShell.Commands.Clear();
            _powerShell.Streams.Error.Clear();
            _powerShell.AddCommand("New-Partition")
                .AddParameter("Number", diskNumber)
                .AddParameter("DriveLetter ", driveLetter)
                .AddParameter("UseMaximumSize");
            var results = await _powerShell.InvokeAsync();
            var operationStatus = _powerShell.Streams.Error.Last().Exception.HResult;
            if (!results.Any())
            {
                // log error.
            }

            return operationStatus;
        }
        catch (Exception)
        {
            // log error and throw
            throw;
        }
    }

    public async Task<int> FormatPartitionAsDevDrive(char driveLetter, string label)
    {
        try
        {
            _powerShell.Commands.Clear();
            _powerShell.Streams.Error.Clear();
            _powerShell.AddCommand("Format-Volume")
                .AddParameter("DriveLetter", driveLetter)
                .AddParameter("FileSystem", "ReFS")
                .AddParameter("NewFileSystemLabel", label)
                .AddParameter("DeveloperVolume");
            var results = await _powerShell.InvokeAsync();
            var operationStatus = _powerShell.Streams.Error.Last().Exception.HResult;
            if (!results.Any())
            {
                // log error.
            }

            return operationStatus;
        }
        catch (Exception)
        {
            // log error and throw
            throw;
        }
    }
}
