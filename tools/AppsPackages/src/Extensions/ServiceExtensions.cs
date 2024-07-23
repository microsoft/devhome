// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.AppsPackages.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.Settings.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppsAndPackages(this IServiceCollection services, HostBuilderContext context)
    {
        // View models
        services.AddTransient<AppsPackagesViewModel>();
        return services;
    }
}
