// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace DevHome.Common.Services;

/// <summary>
/// Delegate for a package version condition
/// </summary>
/// <param name="version">Package version</param>
public delegate bool PackageVersionCondition(PackageVersion version);

public interface IPackageManagerService
{
    public Task<bool> IsInstalledAsync(string packageFamilyName, PackageVersionCondition? versionCondition = null);
}
