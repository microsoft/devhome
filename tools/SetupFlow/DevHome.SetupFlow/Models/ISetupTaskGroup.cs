// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// A container for a group of related setup tasks that are always handled together.
/// </summary>
/// <remarks>
/// For now, each group contains a single type of tasks. For example, only app installation
/// or only repo cloning. In the future, we expect to have groups with different tasks,
/// like dev volume and WSL. That may require some re-work depending on the requirements,
/// but should work fine if a whole group is always shown together and not differently
/// depending on the context.
/// </remarks>
public interface ISetupTaskGroup
{
    /// <summary>
    /// Gets the view model for the setup page containing all the options for this setup task group.
    /// </summary>
    public SetupPageViewModelBase GetSetupPageViewModel();

    /// <summary>
    /// Gets the view model for the contents of the tab shown in the review page for this setup task group.
    /// </summary>
    public ReviewTabViewModelBase GetReviewTabViewModel();

    /// <summary>
    /// Gets all the setup tasks that make up this group
    /// </summary>
    public IEnumerable<ISetupTask> SetupTasks
    {
        get;
    }

    /// <summary>
    /// Gets all the DSC tasks that make up this group
    /// </summary>
    public IEnumerable<ISetupTask> DSCTasks
    {
        get;
    }
}
