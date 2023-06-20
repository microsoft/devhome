// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Service for ensuring the AppManagement (aka App Install) page in the
/// SetupFlow is initialized and ready
/// </summary>
public interface IAppManagementInitializer
{
    /// <summary>
    /// Ensure all services and deployments are initialized
    /// </summary>
    public Task InitializeAsync();
}
