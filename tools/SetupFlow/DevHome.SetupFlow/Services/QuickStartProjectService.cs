// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.Services;

public sealed class QuickStartProjectService : IQuickStartProjectService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuickStartProjectService));

    private readonly IExtensionService _extensionService;
    private readonly ISetupFlowStringResource _setupFlowStringResource;

    public QuickStartProjectService(IExtensionService extensionService, ISetupFlowStringResource setupFlowStringResource)
    {
        _extensionService = extensionService;
        _setupFlowStringResource = setupFlowStringResource;
    }

    public async Task<List<QuickStartProjectProvider>> GetQuickStartProjectProvidersAsync()
    {
        var quickStartProjectProvidersFromAllExtensions = new List<QuickStartProjectProvider>();
        var extensions = await _extensionService.GetInstalledExtensionsAsync(ProviderType.QuickStartProject);
        foreach (var extension in extensions)
        {
            try
            {
                var quickStartProjectProviders = (await extension.GetListOfProvidersAsync<IQuickStartProjectProvider2>()).ToList();
                foreach (var quickStartProjectProvider in quickStartProjectProviders)
                {
                    if (quickStartProjectProvider != null)
                    {
                        quickStartProjectProvidersFromAllExtensions.Add(new(quickStartProjectProvider, _setupFlowStringResource, extension.PackageFullName));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to get {nameof(IQuickStartProjectProvider2)} provider from '{extension.PackageFullName}'");
            }
        }

        return quickStartProjectProvidersFromAllExtensions;
    }
}
