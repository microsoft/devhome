// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace DevHome.RepositoryManagement;

internal static class RepositoryActionHelper
{
    /// <summary>
    /// Deleted repositoryRoot and everything under it.
    /// </summary>
    /// <param name="repositoryRoot">The location to delete from.</param>
    /// <remarks>This works even with read-only files.</remarks>
    internal static void DeleteEverything(string repositoryRoot)
    {
        if (!string.IsNullOrEmpty(repositoryRoot)
                && Directory.Exists(repositoryRoot))
        {
            // Cumbersome, but needed to remove read-only files.
            foreach (var repositoryFile in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(repositoryFile, FileAttributes.Normal);
                File.Delete(repositoryFile);
            }

            foreach (var repositoryDirectory in Directory.GetDirectories(repositoryRoot, "*", SearchOption.AllDirectories).Reverse())
            {
                Directory.Delete(repositoryDirectory);
            }

            File.SetAttributes(repositoryRoot, FileAttributes.Normal);
            Directory.Delete(repositoryRoot, false);
        }
    }
}
