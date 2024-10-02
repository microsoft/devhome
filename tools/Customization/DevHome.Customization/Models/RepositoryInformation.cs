// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Customization.Models;

public class RepositoryInformation
{
    public string RepositoryRootPath { get; set; }

    public string SourceControlProviderCLSID { get; set; }

    public string SourceControlProviderDisplayName { get; set; }

    public string SourceControlProviderPackageDisplayName { get; }

    public string RepositoryPathMapping { get; }

    public int Position { get; }

    public int Size { get; }

    public RepositoryInformation(string rootpath, string classId, int position, int size)
    {
        RepositoryRootPath = rootpath;
        SourceControlProviderCLSID = classId;
        var extension = GetExtension(classId);
        SourceControlProviderDisplayName = GetExtensionDisplayName(extension);
        SourceControlProviderPackageDisplayName = GetExtensionPackageDisplayName(extension);
        RepositoryPathMapping = string.Concat(RepositoryRootPath, " ", SourceControlProviderDisplayName);
        Position = position;
        Size = size;
    }

    private IExtensionWrapper? GetExtension(string classId)
    {
        var extensionService = Application.Current.GetService<IExtensionService>();
        var sourceControlExtensions = Task.Run(async () => await extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository)).Result.ToList();

        foreach (var extension in sourceControlExtensions)
        {
            if (extension.ExtensionClassId == classId)
            {
                return extension;
            }
        }

        return null;
    }

    private string GetExtensionDisplayName(IExtensionWrapper? extension)
    {
        return extension?.ExtensionDisplayName ?? new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources").GetLocalized("MenuFlyoutUnregisteredRepository_Content");
    }

    private string GetExtensionPackageDisplayName(IExtensionWrapper? extension)
    {
        return extension?.PackageDisplayName ?? new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources").GetLocalized("MenuFlyoutUnregisteredRepository_Content");
    }
}
