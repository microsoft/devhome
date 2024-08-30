// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;

namespace DevHome.DevDiagnostics.Models;

public class ProcessModuleInfo
{
    public string ModuleName { get; }

    public string FileVersionInfo { get; }

    public string FileVersion { get; }

    public nint BaseAddress { get; }

    public nint EntryPointAddress { get; }

    public int ModuleMemorySize { get; }

    public ProcessModuleInfo(ProcessModule module)
    {
        ModuleName = module.ModuleName;
        try
        {
            FileVersionInfo = module.FileVersionInfo.ToString();
            FileVersion = module.FileVersionInfo.FileVersion ?? string.Empty;
        }
        catch (FileNotFoundException)
        {
            FileVersionInfo = string.Empty;
            FileVersion = string.Empty;
        }

        BaseAddress = module.BaseAddress;
        EntryPointAddress = module.EntryPointAddress;
        ModuleMemorySize = module.ModuleMemorySize;
    }
}
