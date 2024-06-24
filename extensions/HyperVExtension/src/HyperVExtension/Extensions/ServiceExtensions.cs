// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;
using HyperVExtension.Models.VirtualMachineCreation;
using HyperVExtension.Providers;
using HyperVExtension.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;

namespace HyperVExtension.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddHyperVExtensionServices(this IServiceCollection services, HostBuilderContext context)
    {
        // Instances
        services.AddTransient<IWindowsServiceController, WindowsServiceController>();

        // Services
        services.AddHttpClient();
        services.AddSingleton<IComputeSystemProvider, HyperVProvider>();
        services.AddSingleton<HyperVExtension>();
        services.AddSingleton<IHyperVManager, HyperVManager>();
        services.AddSingleton<IWindowsIdentityService, WindowsIdentityService>();
        services.AddSingleton<IVMGalleryService, VMGalleryService>();
        services.AddSingleton<IArchiveProviderFactory, ArchiveProviderFactory>();
        services.AddSingleton<IDownloaderService, DownloaderService>();

        // Pattern to allow multiple non-service registered interfaces to be used with registered interfaces during construction.
        services.AddSingleton<IPowerShellService>(psService =>
            ActivatorUtilities.CreateInstance<PowerShellService>(psService, new PowerShellSession()));
        services.AddSingleton<HyperVVirtualMachineFactory>(serviceProvider => psObject => ActivatorUtilities.CreateInstance<HyperVVirtualMachine>(serviceProvider, psObject));
        services.AddSingleton<VmGalleryCreationOperationFactory>(serviceProvider => parameters => ActivatorUtilities.CreateInstance<VMGalleryVMCreationOperation>(serviceProvider, parameters));

        return services;
    }
}
