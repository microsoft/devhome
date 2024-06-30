// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using WSLExtension.Contracts;
using WSLExtension.Models;
using static Serilog.Log;

namespace WSLExtension.Services;

public class ProcessCreator : IProcessCreator
{
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
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
            },
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var errors = process.StandardError.ReadToEnd();

        process.WaitForExit();
        var exitCode = process.ExitCode;
        return new WslProcessData(exitCode, output, errors);
    }

    public WslProcessData CreateProcessWithoutWindow(
        string fileName,
        string arguments,
        DataReceivedEventHandler stdOutputHandler,
        DataReceivedEventHandler stdErrorHandler)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode,
            },
            EnableRaisingEvents = true,
        };

        // subscribe to the event handlers
        process.OutputDataReceived += stdOutputHandler;
        process.ErrorDataReceived += stdErrorHandler;

        // The line reads are streams so starting and then calling them after does not
        // make us miss potential outputs and errors. At most our reading may be delayed.
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();
        process.CancelOutputRead();
        process.CancelErrorRead();

        // unsubscribe to the event handlers
        process.OutputDataReceived -= stdOutputHandler;
        process.ErrorDataReceived -= stdErrorHandler;
        return new WslProcessData(process.ExitCode);
    }
}
