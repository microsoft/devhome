// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Common.Models;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Interface for objects that will be a view model to a Dev Drive window.
/// </summary>
public interface IDevDriveWindowViewModel
{
    /// <summary>
    /// Gets the Dev Drive associated with the view model.
    /// </summary>
    public IDevDrive AssociatedDrive
    {
        get;
    }

    /// <summary>
    /// Method that gets called when the Dev Drive Manager has requested the view model to launch a Dev Drive window.
    /// </summary>
    /// <returns> A bool to tell requester if the operation was successful</returns>
    public Task<bool> LaunchDevDriveWindow();
}
