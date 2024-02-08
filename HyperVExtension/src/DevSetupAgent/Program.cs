// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using HyperVExtension.DevSetupAgent;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DevAgent";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<DevAgentService>();
        services.AddSingleton<DevAgentService>();
        services.AddSingleton<IRequestFactory, RequestFactory>();
        services.AddSingleton<IRegistryChannelSettings, RegistryChannelSettings>();
        services.AddSingleton<IHostChannel, HostRegistryChannel>();
        services.AddSingleton<IRequestManager, RequestManager>();
    })
    .Build();

host.Run();
