// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Models;

namespace DevHome.Common.Services;

/// <summary>
/// Enum validation results when the Dev Drive manager performs a validates
/// a Dev Drive. This is only to allow us to know which error to show in the UI.
/// These do not replace established error coding system.
/// </summary>
public enum DevDriveValidationResult
{
    Successful,
    ObjectWasNull,
    InvalidDriveSize,
    InvalidDriveLabel,
    InvalidFolderLocation,
    FileNameAlreadyExists,
    DriveLetterNotAvailable,
    NoDriveLettersAvailable,
    NotEnoughFreeSpace,
}

/// <summary>
/// Interface for Dev Drive manager. Managers should be able to associate the Dev Drive that it creates to a
/// Dev drive window that is launched.
/// </summary>
public interface IDevDriveManager
{
    /// <summary>
    /// Allows objects to request a Dev Drive window be created.
    /// </summary>
    /// <param name="devDrive">Dev Drive the window will be created for</param>
    /// <returns>Returns true if the Dev Drive window was launched successfully</returns>
    public Task<bool> LaunchDevDriveWindow(IDevDrive devDrive);

    /// <summary>
    /// Allows objects to notify the Dev Drive Manager that a Dev Drive window was closed.
    /// </summary>
    /// <param name="newDevDrive">Dev Drive object</param>
    public void NotifyDevDriveWindowClosed(IDevDrive newDevDrive);

    /// <summary>
    /// Gets a new Dev Drive object.
    /// </summary>
    /// <returns>
    /// The Dev Drive to be created
    /// </returns>
    public IDevDrive GetNewDevDrive();

    /// <summary>
    /// Gets a list of all Dev Drives currently on the local system.
    /// These Dev Drives have their DevDriveState set to Exists.
    /// </summary>
    public IEnumerable<IDevDrive> GetAllDevDrivesThatExistOnSystem();

    /// <summary>
    /// Event that requesters can subscribe to, to know when a Dev Drive window has closed.
    /// </summary>
    public event EventHandler<IDevDrive> ViewModelWindowClosed;

    /// <summary>
    /// Validates the values inside the Dev Drive against Dev Drive requirements. A Dev drive is only validated
    /// if the only result returned is DevDriveValidationResult.Successful
    /// </summary>
    /// <param name="devDrive">Dev Drive object</param>
    /// <returns>
    /// A set of operation results from the Dev Drive manager attempting to validate the contents
    /// of the Dev Drive.
    /// </returns>
    public ISet<DevDriveValidationResult> GetDevDriveValidationResults(IDevDrive devDrive);

    /// <summary>
    /// Gets a list of drive letters that have been marked for creation by the Dev Drive Manager.
    /// </summary>
    /// <returns>A list of IDevDrive objects that will be created</returns>
    public IList<IDevDrive> DevDrivesMarkedForCreation
    {
        get;
    }

    /// <summary>
    /// Gets all available drive letters on the system. From these letters, those that are currently
    /// being used by a Dev Drive created in memory by the Dev Drive manager are removed.
    /// </summary>
    /// <param name="usedLetterToKeepInList">
    /// when not null the Dev Drive manager should add the letter in usedLetterToKeepInList even if it
    /// is in used by a Dev Drive in memory.
    /// </param>
    /// <returns>
    /// A list of sorted drive letters currently not in use by the Dev Drive manager and the system
    /// </returns>
    public IList<char> GetAvailableDriveLetters(char? usedLetterToKeepInList = null);

    /// <summary>
    /// Allows objects who hold a IDevDrive object to request that the Manager tell the view model to close the
    /// Dev Drive window. In this case the requester wants to close the window, whereas in the NotifyDevDriveWindowClosed case
    /// the view model is telling the requester the window closed.
    /// </summary>
    /// <param name="devDrive">Dev Drive object</param>
    public void RequestToCloseDevDriveWindow(IDevDrive devDrive);

    /// <summary>
    /// Event that the Dev Drive view model can subscribe to, to know if a requester wants them to close the window, without the user explicitly
    /// closing the window themselves, through actions like clicking the close button.
    /// </summary>
    public event EventHandler<IDevDrive> RequestToCloseViewModelWindow;

    /// <summary>
    /// Removes all Dev Drives that were created in memory by the Dev Drive Manager. This does not detach
    /// or remove a Dev Drive from the users machine.
    /// </summary>
    public void RemoveAllDevDrives();

    /// <summary>
    /// Allows the Dev Drive manager to subscribe to events where changes to a Dev Drive object were cancelled.
    /// </summary>
    public void CancelChangesToDevDrive();

    /// <summary>
    /// Allows the Dev Drive manager to subscribe to events where changes to a Dev Drive object were made.
    /// </summary>
    public void ConfirmChangesToDevDrive();

    /// <summary>
    /// Allows the Dev Drive manager to increase the amount of repositories that will be using the Dev Drive to clone to.
    /// </summary>
    /// <param name="count">the amount to increase by</param>
    public void IncreaseRepositoriesCount(int count);

    /// <summary>
    /// Allows the Dev Drive manager to reduce the amount of repositories that will be using the Dev Drive to clone to.
    /// </summary>
    /// <remarks>
    /// When this value is 0 the dev Drive manager will clear the Dev Drive task group, as there are no items that
    /// need to use the Dev drive. No Dev Drive will be created when the task group is empty.
    /// </remarks>
    public void DecreaseRepositoriesCount();

    /// <summary>
    /// Gets the amount of times the Dev Drive object is being used.
    /// </summary>
    /// <remarks>
    /// When this count goes to zero, we clear the view models task group, as there are no repositories that
    /// will be cloned to the Dev Drive.
    /// </remarks>
    public int RepositoriesUsingDevDrive
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating the set of drive letters currently in use by Dev Drives on the system.
    /// </summary>
    public HashSet<char> DriveLettersInUseByDevDrivesCurrentlyOnSystem
    {
        get;
    }
}
