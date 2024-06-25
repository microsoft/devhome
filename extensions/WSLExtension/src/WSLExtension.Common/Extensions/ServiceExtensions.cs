// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WSLExtension.Common.Extensions;

/// <summary>
/// A class that contains extension methods for <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// Used to return common services for all projects.
/// </remarks>
public static class ServiceExtensions
{
    public static IServiceCollection AddCommonProjectServices(this IServiceCollection services, HostBuilderContext context)
    {
        // Services
        services.AddSingleton<IStringResource, StringResource>();

        return services;
    }
}
