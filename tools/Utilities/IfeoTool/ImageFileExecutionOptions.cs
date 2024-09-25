// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common;
using DevHome.IfeoTool.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Win32;

namespace DevHome.IfeoTool;

[Flags]
public enum IfeoFlags : uint
{
    // These flags are defined in the Windows SDK in ntexapi.h.  The DDK documentation is the best source of information though.
    // https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/global-flag-reference
    None = 0,
    StopOnException = 0x00000001,
    ShowLdrSnaps = 0x00000002,
    DebugInitialCommand = 0x00000004,
    StopOnHungGui = 0x00000008,

    HeapEnableTailCheck = 0x00000010,
    HeapEnableFreeCheck = 0x00000020,
    HeapValidateParameters = 0x00000040,
    HeapValidateAll = 0x00000080,

    ApplicationVerifier = 0x00000100,
    MonitorSilentProcessExit = 0x00000200,
    PoolEnableTagging = 0x00000400,
    HeapEnableTagging = 0x00000800,

    UserModeStackTraceDB = 0x00001000,
    KernelModeStackTraceDB = 0x00002000,
    MaintainObjectTypeList = 0x00004000,
    HeapTaggingByDll = 0x00008000,

    DisableStackExtension = 0x00010000,
    EnableCsrDebug = 0x00020000,
    EnableKDSymbolLoad = 0x00040000,
    DisablePageKernelStacks = 0x00080000,

    EnableSystemCritBreaks = 0x00100000,
    HeapDisableCoalescing = 0x00200000,
    EnableCloseExceptions = 0x00400000,
    EnableExceptionLogging = 0x00800000,

    EnableHandleTypeTagging = 0x01000000,
    HeapPageAllocs = 0x02000000,
    DebugInitialCommandEx = 0x04000000,
    DisableDebugPrint = 0x08000000,

    CriticalSectionEventCreation = 0x10000000,
    StopOnUnhandledException = 0x20000000,
    EnableHandleExceptions = 0x40000000,
    DisableProtectedDllVerification = 0x80000000,
}

public delegate void GlobalFlagsChangedEventHandler();

public class ImageFileExecutionOptions : IDisposable
{
    private readonly string _imageName = string.Empty;
    private readonly string _ifeoKeyPath = string.Empty;
    private readonly RegistryWatcher _ifeoKeyWatcher;
    private const string IfeoKeyPath = "Software\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options";
    private const string IfeoGlobalFlagValueName = "GlobalFlag";
    private IfeoFlags _globalFlags;
    private bool _disposed;

    private RegistryWatcher? _imageKeyWatcher;

    public event GlobalFlagsChangedEventHandler? GlobalFlagsChanged;

    public ImageFileExecutionOptions(string imageName)
    {
        _imageName = imageName;
        _ifeoKeyPath = $"{IfeoKeyPath}\\{_imageName}";

        _ifeoKeyWatcher = new(Registry.LocalMachine, IfeoKeyPath, InitializeImageKeyWatcher, RegistryNotifyFilterFlags.SubKeyAddRemove);
        _ifeoKeyWatcher.Start();

        InitializeImageKeyWatcher();
    }

    public IfeoFlags GlobalFlags
    {
        get => _globalFlags;

        set
        {
            // N.B. When setting the flags internal to this class, use the _globalFlags field directly to avoid triggering a StoreValue() call.
            if (_globalFlags != value)
            {
                _globalFlags = value;
                StoreValue();
            }
        }
    }

    private void InitializeImageKeyWatcher()
    {
        // Check if the key exists, and initialize the watcher if needed.  We do this because the RegistryWatcher creates the key if it
        // doesn't exist, which is undesirable for the image subkey.
        using var subKey = Registry.LocalMachine.OpenSubKey(_ifeoKeyPath);
        if (subKey != null)
        {
            if (_imageKeyWatcher == null)
            {
                _imageKeyWatcher = new(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), _ifeoKeyPath, ImageKeyRegistryChanged);
                _imageKeyWatcher.Start();
            }
        }
        else if (_imageKeyWatcher != null)
        {
            _imageKeyWatcher.Stop();
            _imageKeyWatcher.Dispose();
            _imageKeyWatcher = null;
        }

        // Always load, since the new state could be that the key was removed.
        LoadValue();
    }

    private void LoadValue()
    {
        if (_ifeoKeyPath == string.Empty)
        {
            return;
        }

        using var key = Registry.LocalMachine.OpenSubKey(_ifeoKeyPath);
        if (key != null)
        {
            var value = (int?)key.GetValue(IfeoGlobalFlagValueName);
            if (value != null)
            {
                _globalFlags = (IfeoFlags)(uint)value;

                // Return immediately to ensure all other code paths reset the flags to None.
                return;
            }
        }

        _globalFlags = IfeoFlags.None;
    }

    private void StoreValue()
    {
        IfeoToolApp.Log<IfeoToolGlobalFlagsChanged>("IfeoToolApp_ImageFileExecutionOptions_GlobalFlagsChanged", LogLevel.Critical, new IfeoToolGlobalFlagsChanged(GlobalFlags));

        using var key = Registry.LocalMachine.OpenSubKey(_ifeoKeyPath, true);
        if (key != null)
        {
            key.SetValue(IfeoGlobalFlagValueName, GlobalFlags, RegistryValueKind.DWord);
        }
        else if (GlobalFlags != IfeoFlags.None)
        {
            // Only create the key if we have flags to store.
            using var newKey = Registry.LocalMachine.CreateSubKey(_ifeoKeyPath, true, RegistryOptions.Volatile);
            if (newKey != null)
            {
                newKey.SetValue(IfeoGlobalFlagValueName, GlobalFlags, RegistryValueKind.DWord);
            }
        }
    }

    // If a child key was added or removed, try to initialize the image key watcher.
    private void IfeoKeyChanged() => InitializeImageKeyWatcher();

    private void ImageKeyRegistryChanged()
    {
        LoadValue();
        GlobalFlagsChanged?.Invoke();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _ifeoKeyWatcher.Stop();
                _ifeoKeyWatcher.Dispose();

                _imageKeyWatcher?.Stop();
                _imageKeyWatcher?.Dispose();
            }

            _disposed = true;
        }
    }
}
