// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WSLExtension.Helpers;

public class PackageHelper
{
    private const double Height = 128;

    private const double Width = 128;

    private readonly PackageManager _packageManager = new();

    public virtual bool IsPackageInstalled(string packageName)
    {
        var currentPackage = _packageManager.FindPackagesForUser(string.Empty, packageName).FirstOrDefault();
        return currentPackage != null;
    }

    public async virtual Task<IRandomAccessStreamWithContentType?> GetPackageIconAsRandomAccessStreamAsync(
        string? packageName,
        double width = Width,
        double height = Height)
    {
        // We'll use the first installed distribution for the package family to get the icon.
        var package = _packageManager.FindPackagesForUser(string.Empty, packageName).FirstOrDefault();
        if (package == null)
        {
            return default;
        }

        var stream = package.GetLogoAsRandomAccessStreamReference(new Size(width, height));
        return await stream.OpenReadAsync();
    }
}
