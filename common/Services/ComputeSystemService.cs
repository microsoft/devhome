// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Contracts.Services;
using DevHome.Logging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Services;

public class ComputeSystemService : IComputeSystemService
{
    private readonly IExtensionService _extensionService;
    private readonly IAccountsService _accountService;

    public ComputeSystemService(IExtensionService extensionService, IAccountsService accountService)
    {
        _extensionService = extensionService;
        _accountService = accountService;
    }

    public async Task<IReadOnlyList<Dictionary<IComputeSystemProvider, List<IDeveloperId>>>> GetComputeSystemProvidersAsync()
    {
        var computeSystemProvidersFromAllExtensions = new List<Dictionary<IComputeSystemProvider, List<IDeveloperId>>>();
        var extensions = await _extensionService.GetInstalledExtensionsAsync(ProviderType.ComputeSystem);
        foreach (var extension in extensions)
        {
            try
            {
                var computeSystemProviders = await extension.GetListOfProvidersAsync<IComputeSystemProvider>();
                var extensionObj = extension.GetExtensionObject();
                var devIdList = new List<IDeveloperId>();
                if (extensionObj != null && computeSystemProviders.FirstOrDefault() != null)
                {
                    devIdList.AddRange(_accountService.GetDeveloperIds(extensionObj));
                }

                // Only add non-null providers to the list.
                for (var i = 0; i < computeSystemProviders.Count(); i++)
                {
                    if (computeSystemProviders.ElementAt(i) != null)
                    {
                        computeSystemProvidersFromAllExtensions.Add(new() { { computeSystemProviders.ElementAt(i), devIdList } });
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Logger?.ReportError($"Failed to get {nameof(IComputeSystemProvider)} provider from '{extension.Name}'", ex);
            }
        }

        return computeSystemProvidersFromAllExtensions;
    }
}
