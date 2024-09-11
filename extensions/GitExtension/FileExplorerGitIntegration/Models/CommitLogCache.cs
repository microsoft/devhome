// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace FileExplorerGitIntegration.Models;

internal sealed class CommitLogCache
{
    private readonly string _workingDirectory;

    // For now, we'll use the command line to get the last commit for a file, on demand.
    private readonly GitDetect _gitDetect = new();
    private readonly bool _gitInstalled;

    private readonly LruCacheDictionary<string, CommitWrapper> _cache = new();

    public CommitLogCache(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        _gitInstalled = _gitDetect.DetectGit();
    }

    public CommitWrapper? FindLastCommit(string relativePath)
    {
        if (_cache.TryGetValue(relativePath, out var cachedCommit))
        {
            return cachedCommit;
        }

        var result = FindLastCommitUsingCommandLine(relativePath);

        if (result != null)
        {
            result = _cache.GetOrAdd(relativePath, result);
        }

        return result;
    }

    private CommitWrapper? FindLastCommitUsingCommandLine(string relativePath)
    {
        var result = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), _workingDirectory, $"log -n 1 --pretty=format:%s%n%an%n%ae%n%aI%n%H -- {relativePath}");
        if ((result.Status != Microsoft.Windows.DevHome.SDK.ProviderOperationStatus.Success) || (result.Output is null))
        {
            return null;
        }

        var parts = result.Output.Split('\n');
        if (parts.Length != 5)
        {
            return null;
        }

        string message = parts[0];
        string authorName = parts[1];
        string authorEmail = parts[2];
        DateTimeOffset authorWhen = DateTimeOffset.Parse(parts[3], null, System.Globalization.DateTimeStyles.RoundtripKind);
        string sha = parts[4];
        return new CommitWrapper(message, authorName, authorEmail, authorWhen, sha);
    }
}
