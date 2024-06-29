// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Common.Contracts;
using DevHome.Services.Core.Contracts;
using Serilog;

namespace DevHome.Services;

public class QuickstartSetupService(IMicrosoftStoreService msStoreService, IPackageDeploymentService packageDeploymentService) : IQuickstartSetupService
{
#if CANARY_BUILD
    private const string AzureExtensionStorePackageId = "9NBVFRMSFXHW";
    private const string AzureExtensionPackageFamilyName = "Microsoft.Windows.DevHomeAzureExtension.Canary_8wekyb3d8bbwe";
#elif STABLE_BUILD
    private const string AzureExtensionStorePackageId = "9MV8F79FGXTR";
    private const string AzureExtensionPackageFamilyName = "Microsoft.Windows.DevHomeAzureExtension_8wekyb3d8bbwe";
#else
    private const string AzureExtensionStorePackageId = "";
    private const string AzureExtensionPackageFamilyName = "";
#endif

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuickstartSetupService));

    private readonly IMicrosoftStoreService _msStoreService = msStoreService;
    private readonly IPackageDeploymentService _packageDeploymentService = packageDeploymentService;

    public bool IsDevHomeAzureExtensionInstalled()
    {
#if CANARY_BUILD || STABLE_BUILD
        var packages = _packageDeploymentService.FindPackagesForCurrentUser(AzureExtensionPackageFamilyName);
        return packages.Any();
#else
        return true;
#endif
    }

    public async Task InstallDevHomeAzureExtensionAsync()
    {
        try
        {
            _log.Information("Installing DevHomeAzureExtension");
            await _msStoreService.TryInstallPackageAsync(AzureExtensionStorePackageId);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Installing DevHomeAzureExtension failed");
        }
    }
}
