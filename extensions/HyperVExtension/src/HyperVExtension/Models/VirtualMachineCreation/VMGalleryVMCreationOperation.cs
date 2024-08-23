// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;
using HyperVExtension.Exceptions;
using HyperVExtension.Helpers;
using HyperVExtension.Models.VMGalleryJsonToClasses;
using HyperVExtension.Services;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using Windows.Storage;

namespace HyperVExtension.Models.VirtualMachineCreation;

public delegate VMGalleryVMCreationOperation VmGalleryCreationOperationFactory(VMGalleryCreationUserInput parameters);

/// <summary>
/// Class that represents the VM gallery VM creation operation.
/// </summary>
public sealed class VMGalleryVMCreationOperation : IVMGalleryVMCreationOperation, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", ComponentName);

    private const string ComponentName = nameof(VMGalleryVMCreationOperation);

    private readonly IArchiveProviderFactory _archiveProviderFactory;

    private readonly IHyperVManager _hyperVManager;

    private readonly IDownloaderService _downloaderService;

    private readonly string _tempFolderSaveLocation = Path.GetTempPath();

    private readonly IStringResource _stringResource;

    private readonly IVMGalleryService _vmGalleryService;

    private readonly VMGalleryCreationUserInput _userInputParameters;

    private readonly ManualResetEvent _waitForDownloadCompletion = new(false);

    private const uint MaximumCreationAttempts = 2;

    public CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    private readonly object _lock = new();

    private IOperationReport? _lastDownloadReport;

    private bool _disposed;

    public bool IsOperationInProgress { get; private set; }

    public bool IsOperationCompleted { get; private set; }

    public StorageFile? ArchivedFile { get; private set; }

    public VMGalleryImage Image { get; private set; } = new();

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemActionRequiredEventArgs>? ActionRequired = (s, e) => { };

    public event TypedEventHandler<ICreateComputeSystemOperation, CreateComputeSystemProgressEventArgs>? Progress;

    public VMGalleryVMCreationOperation(
        IStringResource stringResource,
        IVMGalleryService vmGalleryService,
        IDownloaderService downloaderService,
        IArchiveProviderFactory archiveProviderFactory,
        IHyperVManager hyperVManager,
        VMGalleryCreationUserInput parameters)
    {
        _stringResource = stringResource;
        _vmGalleryService = vmGalleryService;
        _userInputParameters = parameters;
        _archiveProviderFactory = archiveProviderFactory;
        _hyperVManager = hyperVManager;
        _downloaderService = downloaderService;
    }

    /// <summary>
    /// Reports the progress of an operation.
    /// </summary>
    /// <param name="value">The archive extraction operation returned by the progress handler which extracts the archive file</param>
    public void Report(IOperationReport value)
    {
        UpdateProgress(value, value.LocalizationKey, $"({Image.Name})");
    }

    /// <summary>
    /// Starts the VM gallery operation.
    /// </summary>
    /// <returns>A result that contains information on whether the operation succeeded or failed</returns>
    public IAsyncOperation<CreateComputeSystemResult> StartAsync()
    {
        return Task.Run(async () =>
        {
            // We attempt to retry creating the VM at least once before completely failing and returning
            // an error back to Dev Home.
            var creationAttempt = 1U;

            while (creationAttempt <= MaximumCreationAttempts)
            {
                try
                {
                    if (creationAttempt < MaximumCreationAttempts)
                    {
                        UpdateProgress(_stringResource.GetLocalized("CreationStarting", $"({_userInputParameters.NewEnvironmentName})"));
                    }
                    else
                    {
                        UpdateProgress(_stringResource.GetLocalized("CreationRetry", $"({_userInputParameters.NewEnvironmentName})"));
                    }

                    var imageList = await _vmGalleryService.GetGalleryImagesAsync();
                    if (imageList.Images.Count == 0)
                    {
                        throw new NoVMImagesAvailableException(_stringResource);
                    }

                    Image = imageList.Images[_userInputParameters.SelectedImageListIndex];

                    await DownloadImageAsync(Image);
                    var virtualMachineHost = _hyperVManager.GetVirtualMachineHost();
                    var absoluteFilePathForVhd = GetUniqueAbsoluteFilePath(virtualMachineHost.VirtualHardDiskPath);

                    // extract the archive file to the destination file.
                    var archiveProvider = _archiveProviderFactory.CreateArchiveProvider(ArchivedFile!.FileType);

                    UpdateProgress(_stringResource.GetLocalized("ExtractionStarting", $"({Image.Name})"));
                    await archiveProvider.ExtractArchiveAsync(this, ArchivedFile!, absoluteFilePathForVhd, CancellationTokenSource.Token);
                    var virtualMachineName = MakeFileNameValid(_userInputParameters.NewEnvironmentName);

                    // Use the Hyper-V manager to create the VM.
                    UpdateProgress(_stringResource.GetLocalized("CreationInProgress", virtualMachineName));
                    var creationParameters = new VirtualMachineCreationParameters(
                        _userInputParameters.NewEnvironmentName,
                        GetVirtualMachineProcessorCount(),
                        absoluteFilePathForVhd,
                        Image.Config.SecureBoot,
                        Image.Config.EnhancedSessionTransportType);

                    return new CreateComputeSystemResult(_hyperVManager.CreateVirtualMachineFromGallery(creationParameters));
                }
                catch (Exception ex)
                {
                    if (creationAttempt == MaximumCreationAttempts)
                    {
                        _log.Error(ex, "Operation to create compute system failed on the last attempt");
                        return new CreateComputeSystemResult(ex, ex.Message, ex.Message);
                    }
                    else
                    {
                        _log.Error(ex, $"Operation to create compute system failed on attempt {creationAttempt}, retrying.");
                    }
                }

                creationAttempt++;
            }

            // We shouldn't get here since we should either complete successfully or throw an error and
            // send that error message back to Dev Home, when we've reached the maximum creation attempts
            // allowed.
            var exception = new InvalidOperationException($"Failed to create VM after two attempts");
            var errorDisplayText = _stringResource.GetLocalized(
                "CreationRetryFailed",
                _userInputParameters.NewEnvironmentName,
                Logging.LogFolderRoot);

            return new CreateComputeSystemResult(exception, errorDisplayText, exception.Message);
        }).AsAsyncOperation();
    }

    private void UpdateProgress(IOperationReport report, string localizedKey, string fileName)
    {
        var bytesReceivedSoFar = BytesHelper.ConvertBytesToString((ulong)report.ProgressObject.BytesReceived);
        var totalBytesToReceive = BytesHelper.ConvertBytesToString((ulong)report.ProgressObject.TotalBytesToReceive);
        var displayString = _stringResource.GetLocalized(localizedKey, fileName, $"{bytesReceivedSoFar} / {totalBytesToReceive}");
        try
        {
            Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(displayString, report.ProgressObject.PercentageComplete));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to update progress");
        }

        if (report.ReportKind == ReportKind.Download)
        {
            _lastDownloadReport = report;
            var progressObject = _lastDownloadReport.ProgressObject;

            if (progressObject.Succeeded || progressObject.Failed)
            {
                _waitForDownloadCompletion.Set();
            }
        }
    }

    private void UpdateProgress(string localizedString, uint percentage = 0u)
    {
        try
        {
            Progress?.Invoke(this, new CreateComputeSystemProgressEventArgs(localizedString, percentage));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to update progress");
        }
    }

    /// <summary>
    /// Downloads the disk image from the Hyper-V VM gallery.
    /// </summary>
    private async Task DownloadImageAsync(VMGalleryImage image)
    {
        var downloadUri = new Uri(Image.Disk.Uri);
        var archivedFileName = _vmGalleryService.GetDownloadedArchiveFileName(Image);
        var archivedFileAbsolutePath = Path.Combine(_tempFolderSaveLocation, archivedFileName);
        var isFileBeingDownloaded = _downloaderService.IsFileBeingDownloaded(archivedFileAbsolutePath);

        // If the file already exists, isn't currently being downloaded and has the correct hash
        // we don't need to download it again.
        if (File.Exists(archivedFileAbsolutePath) && !isFileBeingDownloaded)
        {
            ArchivedFile = await StorageFile.GetFileFromPathAsync(archivedFileAbsolutePath);
            UpdateProgress(_stringResource.GetLocalized("VerifyingHashForExistingFile", $"({image.Name})"));
            if (await _vmGalleryService.ValidateFileSha256Hash(ArchivedFile))
            {
                return;
            }

            // hash is not valid, so we'll delete/overwrite the file and download it again.
            _log.Information("File already exists but hash is not valid. Deleting file and downloading again.");
            UpdateProgress(_stringResource.GetLocalized("HashVerificationFailed", $"({image.Name})"));
            await DeleteFileIfExists(ArchivedFile!);
        }

        UpdateProgress(_stringResource.GetLocalized("DownloadStarting", $"({image.Name})"));
        await _downloaderService.StartDownloadAsync(this, downloadUri, archivedFileAbsolutePath, CancellationTokenSource.Token);

        if (!HasDownloadCompleted())
        {
            // Wait for the last download to complete before moving on.
            _waitForDownloadCompletion.WaitOne();
        }

        if (_lastDownloadReport!.ProgressObject.Failed)
        {
            throw new DownloadOperationFailedException(
                $"Failed to download file due to error: {_lastDownloadReport!.ProgressObject.ErrorMessage}");
        }

        // Create the file to save the downloaded archive image to.
        ArchivedFile = await StorageFile.GetFileFromPathAsync(archivedFileAbsolutePath);

        // Download was successful, we'll check the hash of the file, and if it's valid, we'll extract it.
        UpdateProgress(_stringResource.GetLocalized("VerifyingHashForNewFile", $"({image.Name})"));
        if (!await _vmGalleryService.ValidateFileSha256Hash(ArchivedFile))
        {
            UpdateProgress(_stringResource.GetLocalized("HashVerificationFailed", $"({image.Name})"));
            await ArchivedFile.DeleteAsync();
            throw new DownloadOperationFailedException(_stringResource.GetLocalized("DownloadOperationFailedCheckingHash"));
        }
    }

    private async Task DeleteFileIfExists(StorageFile file)
    {
        try
        {
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to delete file {file.Path}");
        }
    }

    private string MakeFileNameValid(string originalName)
    {
        const string escapeCharacter = "_";
        return string.Join(escapeCharacter, originalName.Split(Path.GetInvalidFileNameChars()));
    }

    private string GetUniqueAbsoluteFilePath(string defaultVirtualDiskPath)
    {
        var extension = Path.GetExtension(Image.Disk.ArchiveRelativePath);
        var expectedExtractedFileLocation = Path.Combine(defaultVirtualDiskPath, $"{_userInputParameters.NewEnvironmentName}{extension}");
        var appendedNumber = 1u;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(expectedExtractedFileLocation);

        // If the extracted virtual hard disk file doesn't exist, we'll extract it to the temp folder.
        // If it does exist we'll need to extract the archive file and append a number to the file
        // as it will be a new file within the temp directory.
        while (File.Exists(expectedExtractedFileLocation))
        {
            expectedExtractedFileLocation = Path.Combine(defaultVirtualDiskPath, $"{fileNameWithoutExtension} ({appendedNumber++}){extension}");
        }

        return expectedExtractedFileLocation;
    }

    private int GetVirtualMachineProcessorCount()
    {
        // We'll use half the number of processors for the processor count of the VM just like VM gallery in Windows.
        return Math.Max(1, Environment.ProcessorCount / 2);
    }

    private bool HasDownloadCompleted()
    {
        var lastProgressReceived = _lastDownloadReport?.ProgressObject;

        if (lastProgressReceived == null)
        {
            return false;
        }

        return lastProgressReceived.Succeeded || lastProgressReceived.Failed;
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _log.Debug("Disposing VMGalleryVMCreationOperation");
            if (disposing)
            {
                _waitForDownloadCompletion.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
