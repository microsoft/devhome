// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Service;
using DevHome.Service.Runtime;

ComHelpers.EnableFastCOMRundown();
ComHelpers.InitializeSecurity();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DevHome Service";
});

builder.Services.AddSingleton<DevHomeService>();
builder.Services.AddHostedService<WindowsBackgroundService>();

IHost host = builder.Build();
host.Run();
