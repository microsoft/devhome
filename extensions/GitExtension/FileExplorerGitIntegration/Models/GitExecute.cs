// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using FileExplorerGitIntegration.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace FileExplorerGitIntegration.Models;

public class GitExecute
{
    public static GitCommandRunnerResultInfo ExecuteGitCommand(string gitApplication, string repositoryDirectory, string command, string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = gitApplication,
                Arguments = $"{command} {arguments}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repositoryDirectory ?? string.Empty,
            };

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return new GitCommandRunnerResultInfo(ProviderOperationStatus.Success, output);
            }
            else
            {
                Log.Error("Failed to start the Git process: process is null");
                return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, "Git process is null", string.Empty, new InvalidOperationException("Failed to start the Git process: process is null"), null);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to invoke Git with arguments: {Argument}", arguments);
            return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, "Failed to invoke Git with arguments", string.Empty, ex, arguments);
        }
    }
}
