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

    public RepositoryInformation(string rootpath, string classId)
    {
        RepositoryRootPath = rootpath;
        SourceControlProviderCLSID = classId;
        SourceControlProviderDisplayName = GetExtensionDisplayName(classId);
    }

    private string GetExtensionDisplayName(string classId)
    {
        var extensionService = Application.Current.GetService<IExtensionService>();
        var sourceControlExtensions = Task.Run(async () => await extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository)).Result.ToList();

        foreach (var extension in sourceControlExtensions)
        {
            if (extension.ExtensionClassId == classId)
            {
                return extension.ExtensionDisplayName;
            }
        }

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");

        return stringResource.GetLocalized("MenuFlyoutUnregisteredRepository_Content");
    }
}
