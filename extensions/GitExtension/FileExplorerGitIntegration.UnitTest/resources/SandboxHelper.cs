// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace FileExplorerGitIntegration.UnitTest;

internal sealed class SandboxHelper
{
    private readonly Dictionary<string, string> _renames = new()
    {
        { "dot-git", ".git" },
        { "dot-gitmodules", ".gitmodules" },
    };

    public DirectoryInfo ResourcesDirectory { get; private set; }

    public DirectoryInfo DeployedDirectory { get; private set; }

    public SandboxHelper()
    {
        var parentDir = Directory.GetParent(typeof(SandboxHelper).Assembly.Location);
        if (parentDir is null)
        {
            throw new InvalidOperationException("Could not obtain resources directory for sandbox repos");
        }

        ResourcesDirectory = new DirectoryInfo(Path.Combine(parentDir.FullName, "resources"));
        DeployedDirectory = Directory.CreateTempSubdirectory("SandboxHelper.");
    }

    public void Cleanup()
    {
        Directory.Delete(DeployedDirectory.FullName, true);
    }

    public string CreateSandbox(string directory)
    {
        var source = new DirectoryInfo(Path.Combine(ResourcesDirectory.FullName, directory));
        var target = new DirectoryInfo(Path.Combine(DeployedDirectory.FullName, directory));
        CopyRecursive(source, target);

        return target.FullName;
    }

    private void CopyRecursive(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
        {
            CopyRecursive(dir, target.CreateSubdirectory(FixName(dir.Name)));
        }

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, FixName(file.Name)));
        }
    }

    private string FixName(string name)
    {
        _renames.TryGetValue(name, out var newName);
        return newName ?? name;
    }
}
