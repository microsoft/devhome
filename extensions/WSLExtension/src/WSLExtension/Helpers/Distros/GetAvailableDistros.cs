// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.DistroDefinitions;
using WSLExtension.Models;
using WSLExtension.Services;

namespace WSLExtension.Helpers.Distros;

public class GetAvailableDistros
{
    public static async Task<List<DistributionState>> Execute(IProcessCaller processCaller)
    {
        var distrosAvailable = processCaller.CallProcess("wsl", "--list --online", out var exitCode);
        if (exitCode != 0)
        {
            return new List<DistributionState>();
        }

        var distros = ParseDistroList(distrosAvailable);

        var definitionsRead = new List<DistributionState>(await DistroDefinitionsManager.ReadDistroDefinitions());

        return DistroDefinitionsManager.Merge(
            definitionsRead,
            distros.Select(n => new DistributionState(n))
                .ToList())
            .Where(r => r.Name != null).ToList();
    }

    private static List<string> ParseDistroList(string distrosAvailable)
    {
        var distros = new List<string>();
        using var reader = new StringReader(distrosAvailable);

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
