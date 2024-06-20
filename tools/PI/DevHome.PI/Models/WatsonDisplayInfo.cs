// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.PI.Models;

public class WatsonDisplayInfo
{
    public WatsonReport Report { get; }

    private string? _failureBucket;

    public string FailureBucket
    {
        get
        {
            if (_failureBucket is null)
            {
                _analyzeResults = InitializeAnalyzeResults();

                string[] lines = _analyzeResults.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    if (line.Contains("FAILURE_BUCKET_ID:"))
                    {
                        _failureBucket = line.Substring(line.IndexOf(':') + 1).Trim();
                        break;
                    }
                }

                _failureBucket ??= string.Empty;
            }

            Debug.Assert(_failureBucket is not null, "This should be non-null now");
            return _failureBucket;
        }
    }

    private string? _analyzeResults;

    public string AnalyzeResults
    {
        get
        {
            if (_analyzeResults is null)
            {
                _analyzeResults = InitializeAnalyzeResults();
            }

            Debug.Assert(_analyzeResults is not null, "This should be non-null now");
            return _analyzeResults;
        }
    }

    public WatsonDisplayInfo(WatsonReport report)
    {
        Report = report;
    }

    private string InitializeAnalyzeResults()
    {
        if (Report.CrashDumpPath is null || Report.CrashDumpPath == string.Empty)
        {
            return "No crash dump available";
        }

        // Where the analysis file should be....
        var analysisFile = Report.CrashDumpPath + ".analyze";

        if (!File.Exists(analysisFile))
        {
            return "Cab has not been analyzed yet";
        }

        try
        {
            return File.ReadAllText(analysisFile);
        }
        catch
        {
        }

        return "Unable to access data";
    }
}
