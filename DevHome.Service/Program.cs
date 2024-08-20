// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Service;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = ".NET Joke Service";
});

builder.Services.AddSingleton<ProcessNotificationService>();
builder.Services.AddHostedService<WindowsBackgroundService>();

IHost host = builder.Build();
host.Run();
