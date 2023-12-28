// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

public interface IWinGetInstallOperation
{
    public Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package);
}
