// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WSLExtension.Helpers;

public class PackageHelper
{
    // Using 128 because the thumbnail in Dev Homes environments page
    // is 64x64 but Windows packaged app logo sizes go up to 256.
    // Getting the 256px image will help if the OS is being scaled.
    // See: https://learn.microsoft.com/windows/apps/design/style/iconography/app-icon-construction
    private readonly Size _logoDimensions = new(256, 256);

    private readonly PackageManager _packageManager = new();

    public virtual bool IsPackageInstalled(string packageName)
    {
        var currentPackage = _packageManager.FindPackagesForUser(string.Empty, packageName).FirstOrDefault();
        return currentPackage != null;
    }

    public async virtual Task<byte[]> GetPackageIconAsByteArrayAsync(string packageName)
    {
        // We'll use the first installed distribution for the package family to get the icon.
        var package = _packageManager.FindPackagesForUser(string.Empty, packageName).First();
        var stream = package.GetLogoAsRandomAccessStreamReference(_logoDimensions);
        var logoStream = await stream.OpenReadAsync();

        // Convert the stream to a byte array
        var bytesArray = new byte[logoStream.Size];
        await logoStream.ReadAsync(bytesArray.AsBuffer(), (uint)logoStream.Size, InputStreamOptions.None);
        return bytesArray;
    }

    /// <summary>
    /// Converts a path to a distributions logo in the DistributionDefinition.yaml file to its
    /// base64 string representation.
    /// </summary>
    /// <param name="logoFilePath">path to the logo file using the ms-appx:// schema</param>
    /// <returns>A base64 string that represents the logo.</returns>
    public async virtual Task<string> GetBase64StringFromLogoPathAsync(string logoFilePath)
    {
        try
        {
            var uri = new Uri(logoFilePath);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var randomAccessStream = await storageFile.OpenReadAsync();

            // Convert the stream to a byte array
            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);

            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString(), logoFilePath);
            return string.Empty;
        }
    }
}
