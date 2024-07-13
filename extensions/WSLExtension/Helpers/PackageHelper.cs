// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Serilog;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WSLExtension.Helpers;

public class PackageHelper
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(PackageHelper));

    // Using 256 because the thumbnail in Dev Home's environments page
    // is 64x64 but Windows packaged app logo sizes go up to 256.
    // Getting the 256px image helps as it will mean the image will be rounded down instead
    // of up, if there is a scaling change.
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
        if (!(GetPackageFromPackageFamilyName(packageName) is Package package))
        {
            return [];
        }

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
            _log.Error(ex, $"Unable to get base64 string from logo file path: {logoFilePath}");
            return string.Empty;
        }
    }

    public virtual Package? GetPackageFromPackageFamilyName(string packageFamilyName)
    {
        return _packageManager.FindPackagesForUser(string.Empty, packageFamilyName).FirstOrDefault();
    }
}
