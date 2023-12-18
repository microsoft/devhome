// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services.WinGet;

public interface IWinGetDeployment
{
    public Task<bool> IsAvailableAsync();

    public Task<bool> IsUpdateAvailableAsync();

    public Task<bool> RegisterAppInstallerAsync();
}
