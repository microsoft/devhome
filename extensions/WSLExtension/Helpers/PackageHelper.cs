// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Management.Deployment;

namespace WSLExtension.Helpers;

public class PackageHelper
{
    private readonly PackageManager _packageManager = new();

    public virtual bool IsPackageInstalled(string packageName)
    {
        var currentPackage = _packageManager.FindPackagesForUser(string.Empty, packageName).FirstOrDefault();
        return currentPackage != null;
    }
}
