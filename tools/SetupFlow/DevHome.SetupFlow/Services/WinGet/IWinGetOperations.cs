// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

public interface IWinGetOperations
{
    public Task<InstallPackageResult> InstallPackageAsync(IWinGetPackage package);

    public Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet);

    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);
}
