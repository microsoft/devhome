// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

public interface IWinGetGetPackageOperation
{
    public Task<IList<IWinGetPackage>> GetPackagesAsync(ISet<Uri> packageUriSet);
}
