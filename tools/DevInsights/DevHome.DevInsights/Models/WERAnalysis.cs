// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using DevHome.DevInsights.Helpers;
using Serilog;

namespace DevHome.DevInsights.Models;

// This class holds the WER analysis from a single dump analysis tool
public class WERAnalysis
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WERAnalysis));

    public Tool AnalysisTool { get; private set; }

    public string? Analysis { get; private set; }

    public string? FailureBucket { get; private set; }

    private readonly string _crashDumpPath;

    public WERAnalysis(Tool analysisTool, string crashDumpPath)
    {
        AnalysisTool = analysisTool;
        _crashDumpPath = crashDumpPath;
    }

    public void Run()
    {
        // See if we have a cached analysis
        var analysisFilePath = GetCachedResultsFileName();

        if (File.Exists(analysisFilePath))
        {
            Analysis = File.ReadAllText(analysisFilePath);
        }
        else
        {
            // Generate the analysis
            ToolLaunchOptions options = new();
            options.CommandLineParams = _crashDumpPath;
            options.RedirectStandardOut = true;

            AnalysisTool.Invoke(options);

            if (options.LaunchedProcess is not null)
            {
                string output = options.LaunchedProcess.StandardOutput.ReadToEnd();
                Analysis = output;

                try
                {
                    // Cache these results
                    File.WriteAllText(analysisFilePath, output);
                }
                catch (Exception ex)
                {
                    // If we can't write the file, we'll just ignore it.
                    // We'll just have to re-analyze the next time.
                    _log.Warning("Failed to cache analysis results - " + ex.ToString());
                }
            }
        }

        if (!string.IsNullOrEmpty(Analysis))
        {
            ExternalTool? debuggerTool = AnalysisTool as ExternalTool;

            Debug.Assert(debuggerTool is not null, "We should only be running external tools on dumps");

            // Apply the tool's regular expression to get the failure bucket

            // From MSDN
            // When using System.Text.RegularExpressions to process untrusted input, pass a time -out value to prevent malicious
            // users from causing a denial - of - service attack. A time-out value specifies how long a pattern - matching method
            // should try to find a match before it times out.
            //
            // Only let the regex run for a max of 30 seconds...
            Regex regex = new(debuggerTool.ExtraInfo, RegexOptions.Compiled, new TimeSpan(0, 0, 30));
            Match match = regex.Match(Analysis);

            if (match.Success)
            {
                FailureBucket = match.Groups[1].Value;
            }
        }
    }

    private string GetCachedResultsFileName()
    {
        return _crashDumpPath + "." + AnalysisTool.Name + ".analysisresults";
    }

    public void RemoveCachedResults()
    {
        var analysisFilePath = GetCachedResultsFileName();

        if (File.Exists(analysisFilePath))
        {
            try
            {
                File.Delete(analysisFilePath);
            }
            catch (Exception ex)
            {
                _log.Warning("Failed to delete cache analysis results - " + ex.ToString());
            }
        }
    }
}
