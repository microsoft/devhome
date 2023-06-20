// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace DevHome.Projects.ViewModels;

public class LauncherViewModel : ObservableObject
{
    public string DisplayName { get; set; }

    public string CommandLine { get; set; }

    public string IconPath { get; set; }

    public Dictionary<string, string> Environment { get; set; } = new ();

    [JsonIgnore]
    public WeakReference<ProjectViewModel> ProjectViewModel { get; set; }

    // this class implements ICreatingProcess and is used to create a process with a custom environment
    // it also implements IServiceProvider to allow the process to query for ICreatingProcess, and it is exposed through COM interop.
    [ComVisible(true)]
    [Guid("D9B1F7B3-3B5F-4B5D-8B5A-8F4F969C4D9E")]
    internal class CreatingProcess : ICreatingProcess, Windows.Win32.System.Com.IServiceProvider
    {
        public Dictionary<string, string> Environment { get; init; } = new ();

        public void OnCreating(ICreateProcessInputs pcpi)
        {
            foreach (var (key, value) in Environment)
            {
                pcpi.SetEnvironmentVariable(key, value);
            }
        }

        public unsafe void QueryService(Guid* guidService, Guid* riid, void** ppvObject)
        {
            if (*guidService == typeof(ICreatingProcess).GUID)
            {
                *ppvObject = (void*)Marshal.GetComInterfaceForObject(this, typeof(ICreatingProcess));
            }
            else if (*guidService == typeof(Windows.Win32.System.Com.IServiceProvider).GUID)
            {
                *ppvObject = (void*)Marshal.GetComInterfaceForObject(this, typeof(Windows.Win32.System.Com.IServiceProvider));
            }
            else
            {
                *ppvObject = null;
                throw new NotImplementedException();
            }
        }
    }

    internal void Launch()
    {
        var cwd = ProjectViewModel?.TryGetTarget(out var p) == true ? p.ExpandedFilePath : null;
        var argv = GetArgv();

        unsafe
        {
            var open = stackalloc char[]
            {
                'o',
                'p',
                'e',
                'n',
                '\0',
            };
            var @params = string.Join(' ', argv.Skip(1));
            fixed (char* file = argv[0], paramsPtr = @params, cwdPtr = cwd)
            {
                var cp = new CreatingProcess { Environment = Environment };
                var cpUnk = Marshal.GetIUnknownForObject(cp);
                try
                {
                    var execInfo = new SHELLEXECUTEINFOW
                    {
                        cbSize = (uint)Marshal.SizeOf<SHELLEXECUTEINFOW>(),
                        lpVerb = new PCWSTR(open),
                        lpFile = file,
                        lpParameters = paramsPtr,
                        lpDirectory = cwdPtr,
                        nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL,
                        fMask = SEE_MASK_FLAG_HINST_IS_SITE,
                        hInstApp = new HINSTANCE(cpUnk),
                    };

                    PInvoke.ShellExecuteEx(ref execInfo);
                }
                finally
                {
                    Marshal.Release(cpUnk);
                }
            }
        }
    }

    public string[] GetArgv()
    {
        unsafe
        {
            var args = CommandLineToArgv(CommandLine, out var argc);
            try
            {
                if (args == null)
                {
                    throw new InvalidOperationException("Failed to parse command line.");
                }

                var argv = Enumerable.Range(0, argc).Select(i => args[i].ToString()).ToArray();
                return argv;
            }
            finally
            {
                LocalFree(new Windows.Win32.Foundation.HLOCAL((IntPtr)args));
            }
        }
    }
}
