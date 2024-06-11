// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Services.DesiredStateConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.Services.DesiredStateConfiguration.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDSC(this IServiceCollection services)
    {
        services.AddSingleton<IDSC, DSC>();
        services.AddSingleton<IDSCDeployment, DSCDeployment>();
        services.AddSingleton<IDSCOperations, DSCOperations>();
        return services;
    }
}
