// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace DevHome.Services.WindowsPackageManager;

public static class ServiceExtensions
{
    public static IServiceCollection AddWindowsPackageManager(this IServiceCollection services)
    {
        return services;
    }
}
