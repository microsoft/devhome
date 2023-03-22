// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Models;

namespace DevHome.Common.Services;

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
/// Enum Operation results when the Dev Drive manager performs an operation
/// related to a Dev Drive such as validating its contents. This is only to
/// allow us to know which error to show in the UI. These do not replace any
/// established error coding system.
/// </summary>
public enum DevDriveOperationResult
{
    Successful,
    ObjectWasNull,
    InvalidDriveSize,
    InvalidDriveLabel,
    InvalidFolderLocation,
    FolderLocationNotFound,
    DefaultFolderNotAvailable,
    DriveLetterNotAvailable,
    NoDriveLettersAvailable,
    NotEnoughFreeSpace,
    CreateDevDriveFailed,
    DevDriveNotFound,
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
    public Task<DevDriveOperationResult> CreateDevDrive(IDevDrive devDrive);

    /// <summary>
    /// Allows objects to request a Dev Drive window be created. The passed in IDevDrive
    /// must have been created by the Dev Drive manager or the return bool will be false
    /// and the window will not launch.
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
    /// <returns>
    /// An Dev Drive thats associated with a viewmodel and a result that indicates whether the operation
    /// was successful.
    /// </returns>
    public (DevDriveOperationResult, IDevDrive) GetNewDevDrive();

    /// <summary>
    /// Gets a list of all Dev Drives currently on the local system. This will cause storage calls
    /// that may be slow so it is done through a task. These Dev Drives have their DevDriveState set to Exists.
    /// </summary>
    public Task<IEnumerable<IDevDrive>> GetAllDevDrivesThatExistOnSystem();

    /// <summary>
    /// Event that requesters can subscribe to, to know when a Dev Drive window has closed.
    /// </summary>
    public event EventHandler<IDevDrive> ViewModelWindowClosed;

    /// <summary>
    /// Validates the values inside the Dev Drive against Dev Drive requirements. Dev drive is only validated
    /// if the only result returned is DevDriveOperationResult.Successful
    /// </summary>
    /// <param name="devDrive">Dev Drive object</param>
    /// <returns>
    /// A set of operation results from the Dev Drive manager attempting to validate the contents
    /// of the Dev Drive.
    /// </returns>
    public ISet<DevDriveOperationResult> GetDevDriveValidationResults(IDevDrive devDrive);

    /// <summary>
    /// Gets a list of drive letters that have been marked for creation by the Dev Drive Manager.
    /// </summary>
    /// <returns>A list of IDevDrive objects that will be created</returns>
    public IList<IDevDrive> DevDrivesMarkedForCreation
    {
        get;
    }

    /// <summary>
    /// Gets All available drive letters on the system and that haven't been used by the Dev Manager at the
    /// current time. The list is small so using a SortedSet should be fine as there will be very few times
    /// this method would be called.
    /// </summary>
    /// <param name="devDrive">
    /// when not null the Dev Drive manager should only add a used letter if it doesn't already belong
    /// to the IDevDrive object.
    /// </param>
    /// <returns>
    /// A list of sorted drive letters currently not in use by the Dev Drive manager and the system
    /// </returns>
    public IList<char> GetAvailableDriveLetters(IDevDrive devDrive);

    /// <summary>
    /// Removes Dev Drives that were created in memory by the Dev Drive Manager. This does not detach
    /// or remove a Dev Drive from the users machine. This is used only to disassociate a Dev Drive object
    /// from a Dev Drive view model that was created by the Dev Drive manager in memory.
    /// <param name="devDrive">Dev Drive object</param>
    /// <returns>
    /// A result indicating whether the operation was successful.
    /// </returns>
    public DevDriveOperationResult RemoveDevDrive(IDevDrive devDrive);

    /// <summary>
    /// Allows who hold a IDevDrive object to request that the Manager tell the view model who created the window to close the
    /// Dev Drive window. In this case the requester wants to close the window, whereas in the NotifyDevDriveWindowClosed case
    /// the view model is telling the requester the window closed.
    /// </summary>
    /// <param name="devDrive">Dev Drive object</param>
    public void RequestToCloseDevDriveWindow(IDevDrive devDrive);

    /// <summary>
    /// Event that View model can subscribe to, to know if a requester wants them to close the window, without the user explicity
    /// closing the window themselves, through actions like clicking the close button.
    /// </summary>
    public event EventHandler<IDevDrive> RequestToCloseViewModelWindow;
}
