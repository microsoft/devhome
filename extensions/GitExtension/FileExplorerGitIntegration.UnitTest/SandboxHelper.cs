// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

namespace FileExplorerGitIntegration.UnitTest;

internal sealed class SandboxHelper
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(SandboxHelper));

    private readonly Dictionary<string, string> _renames = new()
    {
        { "dot-git", ".git" },
        { "dot-gitmodules", ".gitmodules" },
    };

    public DirectoryInfo ResourcesDirectory { get; private set; }

    public DirectoryInfo DeployedDirectory { get; private set; }

    public SandboxHelper()
    {
        var parentDir = Directory.GetParent(typeof(SandboxHelper).Assembly.Location) ?? throw new InvalidOperationException("Could not obtain resources directory for sandbox repos");
        ResourcesDirectory = new DirectoryInfo(Path.Combine(parentDir.FullName, "resources"));
        DeployedDirectory = Directory.CreateTempSubdirectory("SandboxHelper.");
    }

    public void Cleanup()
    {
        try
        {
            Directory.Delete(DeployedDirectory.FullName, true);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, $"Failed to delete temp directory {DeployedDirectory.FullName}");
            throw;
        }
    }

    public string CreateSandbox(string directory, TestContext testContext)
    {
        var source = new DirectoryInfo(Path.Combine(ResourcesDirectory.FullName, directory));
        var target = new DirectoryInfo(Path.Combine(DeployedDirectory.FullName, directory));
        testContext.WriteLine($"Copying repository from {source.FullName} to {target.FullName}.");
        var count = CopyRecursive(source, target);
        testContext.WriteLine($"Copied {count} items.");

        Assert.AreEqual(source.GetDirectories().Length, target.GetDirectories().Length);
        Assert.AreEqual(source.GetFiles().Length, target.GetFiles().Length);
        return target.FullName;
    }

    private int CopyRecursive(DirectoryInfo source, DirectoryInfo target)
    {
        int copiedFiles = 0;
        foreach (var dir in source.GetDirectories())
        {
            ++copiedFiles;
            CopyRecursive(dir, target.CreateSubdirectory(FixName(dir.Name)));
        }

        foreach (var file in source.GetFiles())
        {
            ++copiedFiles;
            file.CopyTo(Path.Combine(target.FullName, FixName(file.Name)));
        }

        return copiedFiles;
    }

    private string FixName(string name)
    {
        _renames.TryGetValue(name, out var newName);
        return newName ?? name;
    }
}
