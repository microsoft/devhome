// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models;
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
        services.AddSingleton<IComputeSystemProvider, HyperVProvider>();
        services.AddSingleton<HyperVExtension>();
        services.AddSingleton<IHyperVManager, HyperVManager>();
        services.AddSingleton<IWindowsIdentityService, WindowsIdentityService>();

        // Pattern to allow multiple non-service registered interfaces to be used with registered interfaces during construction.
        services.AddSingleton<IPowerShellService>(psService =>
            ActivatorUtilities.CreateInstance<PowerShellService>(psService, new PowerShellSession()));
        services.AddSingleton<HyperVVirtualMachineFactory>(sp => psObject => ActivatorUtilities.CreateInstance<HyperVVirtualMachine>(sp, psObject));

        return services;
    }
}
