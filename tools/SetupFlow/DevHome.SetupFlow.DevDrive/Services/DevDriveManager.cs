// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Utilities;
using DevHome.SetupFlow.DevDrive.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.Vhd;
using Windows.Win32.System.Ioctl;

namespace DevHome.Common.Services;

/// <summary>
/// Class for Dev Drive manager. The Dev Drive manager is the mediator between objects that will be a viewmodel for
/// a Dev Drive window and the objects that requested the window to be launched. Message passing between the two so they do not
/// need to know of eachothers existence. The Dev Drive manager uses a Dictionary object to keep the association between
/// the Dev Drive and the Dev Drive window.
/// </summary>
public class DevDriveManager : IDevDriveManager
{
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly IDevDriveStorageOperator _devDriveStorageOperator = new DevDriveStorageOperator();

    /// <summary>
    /// Dictionary that Associates a Dev Drive object with a view model.
    /// </summary>
    private readonly Dictionary<IDevDrive, IDevDriveWindowViewModel> _devDriveToViewModelMap = new ();

    /// <summary>
    /// Event that requesters can subscribe to, to know when a Dev Drive window has closed.
    /// </summary>
    public event EventHandler<DevDriveWindowClosedEventArgs> OnViewModelWindowClosed = (sender, e) => { };

    public DevDriveManager(IHost host, ILogger logger)
    {
        _host = host;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> CreateDevDrive(IDevDrive devDrive)
    {
        // Create and attach the virtual hard disk
        var result = (HRESULT)_devDriveStorageOperator.CreateAndAttachVhd(@"C:\vhds\test.vhdx", 5);
        if (result.Failed)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to create and attach virtual disk. {result.ToString()}");
            return result.Value;
        }

        // Get the disk number to be used to initialize the disk.
        uint diskNumber;
        result = (HRESULT)_devDriveStorageOperator.GetDiskNumber(@"C:\vhds\test.vhdx", out diskNumber);
        if (result.Failed)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to get virtual disk number. {result.ToString()}");
            return result.Value;
        }

        // Initialize the disk
        result = (HRESULT)await _devDriveStorageOperator.InitializeDisk(diskNumber);

        if (result.Failed)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to initialize disk {diskNumber}. {result.ToString()}");
            return result.Value;
        }

        // Create a partition using the provided drive letter.
        result = (HRESULT)await _devDriveStorageOperator.CreatePartition(diskNumber, 'A');

        if (result.Failed)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to create partition disk: {diskNumber}, drive letter: {devDrive.DriveLetter}. {result.ToString()}");
            return result.Value;
        }

        // Format the partition as a Dev Drive.
        result = (HRESULT)await _devDriveStorageOperator.FormatPartitionAsDevDrive('A', "this is a Dev Drive");

        if (result.Failed)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to format partition as Dev Drive: disk: {diskNumber}, drive letter: {devDrive.DriveLetter}. {result.ToString()}");
        }

        return result.Value;
    }

    /// <inheritdoc/>
    public Task<bool> LaunchDevDriveWindow(IDevDrive devDrive)
    {
        try
        {
            return _devDriveToViewModelMap[devDrive].LaunchDevDriveWindow();
        }
        catch (Exception ex)
        {
            _logger.LogError(nameof(DevDriveManager), LogLevel.Info, $"Failed to launch a new Dev Drive window. {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public void NotifyDevDriveWindowClosed(IDevDrive devDrive)
    {
        OnViewModelWindowClosed(null, new DevDriveWindowClosedEventArgs(devDrive));
    }

    /// <summary>
    /// Creates a new dev drive object. This creates a IDevDrive object with prepopulated data. The size,
    /// name, location and drive letter will be prepopulated.
    /// </summary>
    /// <returns>An Dev Drive thats associated with a viewmodel</returns>
    public IDevDrive GetNewDevDrive()
    {
        // TODO: Add validation checks to ensure that we can create a dev drive, with default values.
        // For example, we need to make sure there are drive letters available.
        var newViewModel = _host.GetService<DevDriveViewModel>();
        _devDriveToViewModelMap[newViewModel.AssociatedDrive] = newViewModel;
        return newViewModel.AssociatedDrive;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IDevDrive>> GetAllDevDrivesThatExistOnSystem()
    {
        return Task.Run(() =>
        {
            // TODO: Return empty list so code does not throw.
            // SHould fix with implementation.
            IEnumerable<IDevDrive> toReturn = new List<IDevDrive>();
            return toReturn;
        });
    }
}
