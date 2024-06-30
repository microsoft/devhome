// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.Models;

namespace WSLExtension.Helpers;

public static class WslCommandOutputParser
{
    private const int WslListColumnName = 0;
    private const int WslListColumnState = 1;

    // Confirmed Wsl statuses are not localized.
    private const string WslListStateRunning = "Running";

    private const int WslListColumnVersion = 2;

    public static List<WslDistributionInfo> ParseVerboseDistributionDetails(string wslExeListVerboseDetails)
    {
        var distros = new List<WslDistributionInfo>();
        using (var reader = new StringReader(wslExeListVerboseDetails))
        {
            reader.ReadLine(); // Skip the first line
            while (reader.ReadLine() is { } line)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                // Line starting with an asterisk indicates the distribution is the default distribution
                var isDistroDefault = line.StartsWith('*');

                var parts = line.Substring(2).Split([" "], StringSplitOptions.RemoveEmptyEntries);

                var distribution = new WslDistributionInfo(parts[WslListColumnName])
                {
                    IsDefaultDistribution = isDistroDefault,
                    IsRunning = parts[WslListColumnState] == WslListStateRunning,
                    Version2 = parts[WslListColumnVersion] == "2",
                };

                distros.Add(distribution);
            }
        }

        return distros;
    }

    public static List<string> ParseKnownDistributionsFoundInMsStore(string knownDistributionsInMsStore)
    {
        var distros = new List<string>();
        using var reader = new StringReader(knownDistributionsInMsStore);

        Enumerable.Range(1, 4).ToList().ForEach(_ => reader.ReadLine());

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }

            var parts = line.Split([" "], StringSplitOptions.RemoveEmptyEntries);

            distros.Add(parts[0] == "*" ? parts[1] : parts[0]);
        }

        return distros;
    }
}
