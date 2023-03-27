// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Contracts.Services;

public interface IAccountsService
{
    Task InitializeAsync();

    IReadOnlyList<IDevIdProvider> GetDevIdProviders();

    IReadOnlyList<IDeveloperId> GetDeveloperIds(IDevIdProvider iDevIdProvider);

    IReadOnlyList<IDeveloperId> GetDeveloperIds(IPlugin plugin);
}
