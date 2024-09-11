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
    public static GitCommandRunnerResultInfo ExecuteGitCommand(string gitApplication, string repositoryDirectory, string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo();
            var wslArgument = WslIntegrator.GetArgumentPrefixForWsl(repositoryDirectory);
            if (wslArgument == string.Empty)
            {
                processStartInfo.FileName = gitApplication;
                processStartInfo.Arguments = arguments;
                processStartInfo.WorkingDirectory = repositoryDirectory ?? string.Empty;
            }
            else
            {
                Log.Information("Wsl.exe will be invoked to obtain property information from git");
                processStartInfo.FileName = "wsl.exe";
                processStartInfo.Arguments = string.Concat(wslArgument, arguments);
            }

            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();

                // Add timeout for 1 minute
                process.WaitForExit(TimeSpan.FromMinutes(1));
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
