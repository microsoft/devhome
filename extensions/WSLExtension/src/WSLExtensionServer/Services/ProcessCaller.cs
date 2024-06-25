// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WSLExtension;
using static Serilog.Log;

namespace WSLExtension.Services;

public class ProcessCaller : IProcessCaller
{
    private readonly bool _isWow64;

    public ProcessCaller()
    {
        _isWow64 = IsWow64Process2(GetCurrentProcess(), out _, out _);
    }

    public string CallProcess(string command, string arguments, string? workingDirectory = null)
    {
        return CallProcess(command, arguments, Encoding.Unicode, workingDirectory);
    }

    public string CallProcess(string command, string arguments, out int exitCode, string? workingDirectory = null)
    {
        return CallProcess(command, arguments, Encoding.Unicode, out exitCode, workingDirectory);
    }

    public string CallProcess(
        string command,
        string arguments,
        Encoding outputEncoding,
        out int exitCode,
        string? workingDirectory = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = outputEncoding,
            },
        };

        if (workingDirectory != null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        try
        {
            StartProcess(process);

            var output = process.StandardOutput.ReadToEnd();

            exitCode = process.ExitCode;

            var trimmedOutput = output.ReplaceLineEndings(" \\n ");

            Logger.Information(
                "[ENDED]   ({ProcessId}) {Command} {Arguments} ({ExitCode}): {Output}",
                process.Id,
                command,
                arguments,
                exitCode,
                trimmedOutput);

            return output;
        }
        finally
        {
            process.Dispose();
        }
    }

    public string CallProcess(string command, string arguments, Encoding outputEncoding, string? workingDirectory = null)
    {
        return CallProcess(command, arguments, outputEncoding, out _, workingDirectory);
    }

    public void CallDetachedProcess(string command, string arguments, bool useShell = false)
    {
        Process process;
        if (command != Constants.WtExecutable)
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = useShell,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                },
            };
        }
        else
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c start " + command + " " + arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                },
            };
        }

        try
        {
            StartProcess(process);
        }
        finally
        {
            process.Dispose();
        }
    }

    public string CallElevatedProcess(string command, string arguments, Encoding outputEncoding, string? workingDirectory = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                Verb = "runas",
            },
        };

        if (workingDirectory != null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        try
        {
            StartProcess(process);

            return string.Empty;
        }
        finally
        {
            process.Dispose();
        }
    }

    public string RunCmdInDistro(string distroRegistration, string command, bool root)
    {
        return RunCmdInDistro(distroRegistration, command, out _, root);
    }

    public string RunCmdInDistro(string distroRegistration, string command, out int exitCode, bool root, string? stdIn = null)
    {
        var userRoot = root ? " --user root " : string.Empty;
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "wsl",
            Arguments = "--distribution " + distroRegistration + userRoot + " -- " + command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        var process = new Process
        {
            StartInfo = processStartInfo,
        };

        if (stdIn != null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        try
        {
            StartProcess(process);

            if (process.StartInfo.RedirectStandardInput)
            {
                StreamWriter processStandardInput = process.StandardInput;
                using (processStandardInput)
                {
                    processStandardInput.Write(stdIn);
                }
            }

            var output = process.StandardOutput.ReadToEnd();
            exitCode = process.ExitCode;

            var trimmedOutput = output.ReplaceLineEndings(" \\n ");

            Logger.Information(
                "[ENDED]   ({ProcessId}) {ProcessStartInfo.FileName} {ProcessStartInfo.Arguments} ({ExitCode}): {Output}",
                process.Id,
                processStartInfo.FileName,
                processStartInfo.Arguments,
                exitCode,
                trimmedOutput);

            return output;
        }
        finally
        {
            process.Dispose();
        }
    }

    public string RunCmdInDistroDetached(string distroRegistration, string command, bool root = false, string? stdIn = null)
    {
        var userRoot = root ? " --user root " : string.Empty;
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "wsl",
            Arguments = "--distribution " + distroRegistration + userRoot + " -- " + command,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false,
        };

        var process = new Process
        {
            StartInfo = processStartInfo,
        };
        try
        {
            StartProcess(process);

            return string.Empty;
        }
        finally
        {
            process.Dispose();
        }
    }

    public async Task<int> CallInteractiveProcess(string command, string arguments)
    {
        Process process;
        if (command != Constants.WtExecutable)
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                },
            };
        }
        else
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c start " + command + " " + arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                },
            };
        }

        try
        {
            StartProcess(process);

            await process.WaitForExitAsync(CancellationToken.None);
            return process.ExitCode;
        }
        finally
        {
            process.Dispose();
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(
        IntPtr process,
        out ushort processMachine,
        out ushort nativeMachine);

    private void StartProcess(Process process)
    {
        var ptr = new IntPtr(0);

        var isWow64FsRedirectionDisabled = false;

        if (_isWow64)
        {
            isWow64FsRedirectionDisabled = Wow64DisableWow64FsRedirection(ref ptr);
        }

        try
        {
            process.Start();

            Logger.Information(
                "[STARTED] ({ProcessId}) {Command} {Arguments}",
                process.Id,
                process.StartInfo.FileName,
                process.StartInfo.Arguments);
        }
        finally
        {
            if (isWow64FsRedirectionDisabled)
            {
                Wow64RevertWow64FsRedirection(ptr);
            }
        }
    }
}
