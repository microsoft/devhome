// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Contracts.Services;

public interface IAccountsService
{
    Task InitializeAsync();

    IReadOnlyList<IDevIdProvider> GetDevIdProviders();

    IReadOnlyList<IDeveloperId> GetDeveloperIds(IDevIdProvider iDevIdProvider);

    IReadOnlyList<IDeveloperId> GetDeveloperIds(IPlugin plugin);
}
