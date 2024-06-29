// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WSLExtension;
using WSLExtension.Helpers;
using WSLExtension.Models;
using static Serilog.Log;

namespace WSLExtension.Services;

public class ProcessCaller : IProcessCaller
{
    public ProcessCaller()
    {
    }

    public Process CreateProcessWithWindow(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
            },
        };

        process.Start();
        return process;
    }

    public WslProcessData CreateProcessWithoutWindow(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
            },
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd().ReplaceLineEndings(" \\n ");
        var errors = process.StandardError.ReadToEnd().ReplaceLineEndings(" \\n ");

        process.WaitForExit();
        var exitCode = process.ExitCode;
        return new WslProcessData(exitCode, output, errors);
    }

    public string CallProcess(
        string command,
        string arguments,
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
                StandardOutputEncoding = Encoding.Unicode,
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

    private void StartProcess(Process process)
    {
        process.Start();

        Logger.Information(
            "[STARTED] ({ProcessId}) {Command} {Arguments}",
            process.Id,
            process.StartInfo.FileName,
            process.StartInfo.Arguments);
    }
}
