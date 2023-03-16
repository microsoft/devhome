// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.DevDrive.Models;

namespace DevHome.SetupFlow.DevDrive.Services;

/// <summary>
/// Allows requesters to get passed back the Dev Drive object once a Dev Drive window has closed.
/// </summary>
public class DevDriveWindowClosedEventArgs : EventArgs
{
    public DevDriveWindowClosedEventArgs(IDevDrive devDrive)
    {
        DevDrive = devDrive;
    }

    public IDevDrive DevDrive
    {
        get;
    }
}

/// <summary>
/// Interface for Dev Drive manager. Managers should be able to associate the Dev Drive that it creates to a
/// Dev drive window that is launched.
/// </summary>
public interface IDevDriveManager
{
    /// <summary>
    /// Starts off the Dev Drive creation operations for the requested IDevDrive object.
    /// </summary>
    /// <param name="devDrive">IDevDrive to create</param>
    /// <returns>Returns true if the Dev Drive was created successfully</returns>
    public Task<bool> CreateDevDrive(IDevDrive devDrive);

    /// <summary>
    /// Allows objects to request a Dev Drive window be created.
    /// </summary>
    /// <param name="devDrive">Dev Drive the window will be created for</param>
    /// <returns>Returns true if the Dev Drive window was launched successfully</returns>
    public Task<bool> LaunchDevDriveWindow(IDevDrive devDrive);

    /// <summary>
    /// Allows objects to notify the Dev Drive Manager that a Dev Drive window was closed.
    /// </summary>
    /// <param name="devDrive">Dev Drive object</param>
    public void NotifyDevDriveWindowClosed(IDevDrive devDrive);

    /// <summary>
    /// Gets a new Dev Drive object.
    /// </summary>
    /// <returns>An Dev Drive thats associated with a viewmodel</returns>
    public IDevDrive GetNewDevDrive();

    /// <summary>
    /// Gets a list of all Dev Drives currently on the local system. This will cause storage calls
    /// that may be slow so it is done through a task. These Dev Drives have their DevDriveState set to Exists.
    /// </summary>
    public Task<IEnumerable<IDevDrive>> GetAllDevDrivesThatExistOnSystem();

    /// <summary>
    /// Event that requesters can subscribe to, to know when a Dev Drive window has closed.
    /// </summary>
    public event EventHandler<DevDriveWindowClosedEventArgs> OnViewModelWindowClosed;
}
