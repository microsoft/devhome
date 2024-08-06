// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.RepositoryManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryManagement(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddTransient<RepositoryManagementMainPageViewModel>();
        services.AddTransient<RepositoryManagementItemViewModel>();

        return services;
    }
}
