// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
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
}
