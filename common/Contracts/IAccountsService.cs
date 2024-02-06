// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Contracts.Services;

public interface IAccountsService
{
    Task InitializeAsync();

    Task<IReadOnlyList<IDeveloperIdProvider>> GetDevIdProviders();

    IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider);

    IReadOnlyList<IDeveloperId> GetDeveloperIds(IExtension extension);
}
