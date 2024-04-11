// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel;

namespace DevHome.Common.Services;

public class ComputeSystemService : IComputeSystemService
{
    private const string DevHomePreviewPackageFamilyName = "Microsoft.Windows.DevHome_8wekyb3d8bbwe";

    private const string DevHomeDevPackageFamilyName = "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe";

    private const string DevHomeCanaryPackageFamilyName = "Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe";

    private readonly HashSet<string> _devHomePackageFamilyName = new()
    {
        DevHomePreviewPackageFamilyName,
        DevHomeDevPackageFamilyName,
        DevHomeCanaryPackageFamilyName,
    };

    private readonly IExtensionService _extensionService;

    private readonly IAccountsService _accountService;

    public ComputeSystemService(IExtensionService extensionService, IAccountsService accountService)
    {
        _extensionService = extensionService;
        _accountService = accountService;
    }

    public async Task<List<ComputeSystemProviderDetails>> GetComputeSystemProvidersAsync()
    {
        var computeSystemProvidersFromAllExtensions = new List<ComputeSystemProviderDetails>();
        var extensions = await _extensionService.GetInstalledExtensionsAsync(ProviderType.ComputeSystem);
        foreach (var extension in extensions)
        {
            try
            {
                // Work around for issue where the Dev Home's extension service uses classIds within a package to identify the extension, but doesn't handle
                // multiple packages with the same extension classIds. We need to filter out the Dev Home extensions that are not within the current package.
                // E.g the Hyper-V extension is in Dev Home Dev, Canary and preview builds, each with the same class Id.
                // So Dev Home sees this as 3 separate extensions, causing us to query the same COM server up to 3 times depending on how many of the 3 are
                // installed.
                if (_devHomePackageFamilyName.Contains(extension.PackageFamilyName) &&
                    extension.PackageFamilyName != Package.Current.Id.FamilyName)
                {
                    continue;
                }

                var computeSystemProviders = await extension.GetListOfProvidersAsync<IComputeSystemProvider>();
                var extensionObj = extension.GetExtensionObject();
                var devIdList = new List<DeveloperIdWrapper>();
                if (extensionObj != null && computeSystemProviders.FirstOrDefault() != null)
                {
                    devIdList.AddRange(_accountService.GetDeveloperIds(extensionObj).Select(id => new DeveloperIdWrapper(id)));
                }

                if (devIdList.Count == 0)
                {
                    // If we don't have a developer id for the extension, add an empty one so we can still get the compute systems.
                    devIdList.Add(new DeveloperIdWrapper(new EmptyDeveloperId()));
                }

                // Only add non-null providers to the list.
                for (var i = 0; i < computeSystemProviders.Count(); i++)
                {
                    if (computeSystemProviders.ElementAt(i) != null)
                    {
                        computeSystemProvidersFromAllExtensions.Add(new(extension, new ComputeSystemProvider(computeSystemProviders.ElementAt(i)), devIdList));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to get {nameof(IComputeSystemProvider)} provider from '{extension.PackageFamilyName}/{extension.ExtensionDisplayName}'");
            }
        }

        return computeSystemProvidersFromAllExtensions;
    }
}
