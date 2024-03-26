// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Models.VMGalleryJsonToClasses;
using Windows.Storage;

namespace HyperVExtension.Services;

/// <summary>
/// Interface for creating a service that will handle the VM gallery functionality.
/// </summary>
public interface IVMGalleryService
{
    /// <summary>
    /// Gets the Hyper-V virtual machine gallery data from the web.
    /// </summary>
    /// <returns>An object that represents a list of virtual machine images retrieved from the gallery</returns>
    public Task<VMGalleryImageList> GetGalleryImagesAsync();

    /// <summary>
    /// Used to get the file name of the downloaded archive file that contains the virtual disk. We use the hash of the archive as the file name.
    /// The provided hash is the SHA256 hash of the archive in the form of "sha256:09C50382D496C5DF2C96034EB69F20D456B25308B3F672257D55FD8202DDF84B"
    /// </summary>
    /// <param name="image">An object that represents the VM gallery Image json item</param>
    /// <returns>a string with the SHA256 hex values as the name and the appropriate extension based on the web location of the archive file</returns>
    public string GetDownloadedArchiveFileName(VMGalleryImage image);

    /// <summary>
    /// Validates that the file has the correct SHA256 hash and has not been tampered with.
    /// </summary>
    /// <param name="file">File on the file system that we will compare the hash to</param>
    /// <returns>True if the hash validation was successful and false otherwise</returns>
    public Task<bool> ValidateFileSha256Hash(StorageFile file);

    /// <summary>
    /// Validates that the file has the correct SHA256 hash and has not been tampered with.
    /// </summary>
    /// <param name="byteArray">ByteArray received from the web that we will compare the hash to</param>
    /// <returns>True if the hash validation was successful and false otherwise</returns>
    public bool ValidateFileSha256Hash(byte[] byteArray, string hashOfGalleryItem);
}
