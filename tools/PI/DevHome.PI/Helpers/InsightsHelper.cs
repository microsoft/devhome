// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DevHome.PI.Models;
using Serilog;

namespace DevHome.PI.Helpers;

internal sealed partial class InsightsHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(InsightsHelper));

    // TODO: Add more patterns for different insights.
    // TODO: Insights patterns should be in a database of some kind.
    // TODO: Pattern texts should be extracted from localized windows builds.
    [GeneratedRegex(
        @"The process cannot access the file '(.+?)' because it is being used by another process",
        RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex LockedFileErrorRegex();

    // TODO The following are examples of a simple pattern where we map error code to some help text.
    // This is temporary: longer-term, we should update the errors.db
    // to map the error code to a description, plus any existing documented solution options.
    [GeneratedRegex(@"0xc0000409", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex BufferOverflowErrorRegex();

    [GeneratedRegex(@"0xc0000005", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex MemoryErrorRegex();

    private static readonly List<InsightRegex> RegexList = [];

    static InsightsHelper()
    {
        RegexList.Add(new InsightRegex(InsightType.LockedFile, LockedFileErrorRegex()));
        RegexList.Add(new InsightRegex(InsightType.Security, BufferOverflowErrorRegex()));
        RegexList.Add(new InsightRegex(InsightType.MemoryViolation, MemoryErrorRegex()));
    }

    internal static Insight? FindPattern(string errorText)
    {
        Insight? newInsight = null;

        foreach (var insightRegex in RegexList)
        {
            var match = insightRegex.Regex.Match(errorText);
            if (match.Success)
            {
                newInsight = new Insight
                {
                    InsightType = insightRegex.InsightType,
                };

                // Once we flesh out our error database, we should have a more structured way to
                // handle different types of insights, rather than a switch statement.
                switch (insightRegex.InsightType)
                {
                    case InsightType.LockedFile:
                        {
                            // Extract the file path from the matched group.
                            var pattern = string.Empty;
                            if (match.Groups != null && match.Groups.Count > 1)
                            {
                                pattern = match.Groups[1].Value;
                            }

                            newInsight.Title = CommonHelper.GetLocalizedString("LockedFileInsightTitle");
                            var processName = GetLockingProcess(pattern);
                            if (!string.IsNullOrEmpty(processName))
                            {
                                newInsight.Description =
                                    CommonHelper.GetLocalizedString("LockedFileInsightSpecificDescription", pattern, processName);
                            }
                            else
                            {
                                newInsight.Description =
                                    CommonHelper.GetLocalizedString("LockedFileInsightUnknownDescription", pattern);
                            }
                        }

                        break;

                    case InsightType.Security:
                        {
                            var hexValue = match.Value;
                            var intConverter = new Int32Converter();
                            var errorAsInt = (int?)intConverter.ConvertFromString(hexValue);
                            if (errorAsInt is not null)
                            {
                                var errors = ErrorLookupHelper.LookupError((int)errorAsInt);
                                if (errors is not null && errors.Length > 0)
                                {
                                    var error = errors[0];
                                    {
                                        newInsight.Description =
                                            CommonHelper.GetLocalizedString("GenericInsightDescription", error.Name, error.Help);
                                    }
                                }
                            }

                            newInsight.Title = CommonHelper.GetLocalizedString("SecurityInsightTitle");
                        }

                        break;

                    case InsightType.MemoryViolation:
                        {
                            var hexValue = match.Value;
                            var intConverter = new Int32Converter();
                            var errorAsInt = (int?)intConverter.ConvertFromString(hexValue);
                            if (errorAsInt is not null)
                            {
                                var errors = ErrorLookupHelper.LookupError((int)errorAsInt);
                                if (errors is not null && errors.Length > 0)
                                {
                                    var error = errors[0];
                                    {
                                        if (IsPythonCtypesError(errorText, out var description))
                                        {
                                            newInsight.Description = description;
                                        }
                                        else
                                        {
                                            newInsight.Description =
                                                CommonHelper.GetLocalizedString("GenericInsightDescription", error.Name, error.Help);
                                        }
                                    }
                                }
                            }

                            newInsight.Title = CommonHelper.GetLocalizedString("MemoryInsightTitle");
                        }

                        break;

                    default:
                        break;
                }

                break;
            }
        }

        return newInsight;
    }

    // This is an example of an error that requires additional runtime processing to
    // determine the locking process, so it cannot be handled in the error database alone.
    private static string GetLockingProcess(string lockedFilePath)
    {
        var lockingProcess = string.Empty;

        try
        {
            // Determines if the specified file is locked by another process.
            _ = RestartManagerHelper.GetLockingProcesses(lockedFilePath, out var processes);
            if (processes != null && processes.Count > 0)
            {
                var process = processes[0];
                lockingProcess = process.ProcessName;
            }
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Unable to determine if process is locked.");
        }

        return lockingProcess;
    }

    // We're special-casing Python ctypes errors here, just to exercise this type of issue
    // pattern, but longer-term this should be handled by some data relationship in the errors.db.
    private static bool IsPythonCtypesError(string errorText, out string description)
    {
        var result = false;
        description = string.Empty;
        var appPathPattern = @"Faulting application path: .*\\python\.exe";
        var modulePathPattern = @"Faulting module path: .*\\_ctypes\.pyd";
        var hasAppPath = Regex.IsMatch(errorText, appPathPattern);
        var hasModulePath = Regex.IsMatch(errorText, modulePathPattern);

        if (hasAppPath && hasModulePath)
        {
            description = CommonHelper.GetLocalizedString("PythonCtypesDescription");
            result = true;
        }

        return result;
    }
}
