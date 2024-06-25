// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using WSLExtension.Models;

namespace WSLExtension.Helpers;

public class WslCommandUtils
{
    private const int WslListColumnName = 0;
    private const int WslListColumnState = 1;

    private const string WslListStateStopped = "Stopped";
    private const string WslListStateRunning = "Running";

    private const int WslListColumnVersion = 2;

    public static List<Distro> ParseDistroListDetail(string distroListDetail)
    {
        var distros = new List<Distro>();
        using (var reader = new StringReader(distroListDetail))
        {
            reader.ReadLine(); // Skip the first line
            while (reader.ReadLine() is { } line)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var distroDefault = line.StartsWith('*');

                var parts = line.Substring(2).Split([" "], StringSplitOptions.RemoveEmptyEntries);

                var distro = new Distro(parts[WslListColumnName])
                {
                    DefaultDistro = distroDefault,
                    Running = parts[WslListColumnState] == WslListStateRunning,
                    Version2 = parts[WslListColumnVersion] == "2",
                };

                distros.Add(distro);
            }
        }

        return distros;
    }
}
