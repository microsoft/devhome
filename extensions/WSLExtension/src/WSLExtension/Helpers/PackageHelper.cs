// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Windows.DevHome.SDK;
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

    public async virtual Task<byte[]> GetPackageIconAsByteArrayAsync(
        string packageName,
        double width = Width,
        double height = Height)
    {
        // We'll use the first installed distribution for the package family to get the icon.
        var package = _packageManager.FindPackagesForUser(string.Empty, packageName).First();
        var stream = package.GetLogoAsRandomAccessStreamReference(new Size(width, height));
        var logoStream = await stream.OpenReadAsync();

        // Convert the stream to a byte array
        var bytesArray = new byte[logoStream.Size];
        await logoStream.ReadAsync(bytesArray.AsBuffer(), (uint)logoStream.Size, InputStreamOptions.None);
        return bytesArray;
    }
}
