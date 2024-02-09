// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Service for ensuring the AppManagement (aka App Install) page in the
/// SetupFlow is initialized and ready
/// </summary>
public interface IAppManagementInitializer
{
    /// <summary>
    /// Initialize app management services
    /// </summary>
    public Task InitializeAsync();

    /// <summary>
    /// Reinitialize app management services
    /// </summary>
    public Task ReinitializeAsync();
}
