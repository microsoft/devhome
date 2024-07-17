// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel;

namespace DevHome.Environments.TestModels;

public class TestExtensionWrapper : IExtensionWrapper
{
    public string PackageDisplayName => throw new NotImplementedException();

    public string ExtensionDisplayName => throw new NotImplementedException();

    public string PackageFullName => "Microsoft.Windows.DevHome.Dev_0.0.0.0_x64__8wekyb3d8bbwe";

    public string PackageFamilyName => throw new NotImplementedException();

    public string Publisher => throw new NotImplementedException();

    public string ExtensionClassId => throw new NotImplementedException();

    public DateTimeOffset InstalledDate => throw new NotImplementedException();

    public PackageVersion Version => throw new NotImplementedException();

    public string ExtensionUniqueId => throw new NotImplementedException();

    public void AddProviderType(ProviderType providerType) => throw new NotImplementedException();

    public IExtension? GetExtensionObject() => throw new NotImplementedException();

    public Task<IEnumerable<T>> GetListOfProvidersAsync<T>()
        where T : class
        => throw new NotImplementedException();

    public Task<T?> GetProviderAsync<T>()
        where T : class
        => throw new NotImplementedException();

    public bool HasProviderType(ProviderType providerType) => throw new NotImplementedException();

    public bool IsRunning() => throw new NotImplementedException();

    public void SignalDispose() => throw new NotImplementedException();

    public Task StartExtensionAsync() => throw new NotImplementedException();
}
