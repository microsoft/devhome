// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ElevatedComponent.Helpers;
using DevHome.SetupFlow.ElevatedComponent.Tasks;
using Windows.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Class for executing tasks that run in the elevated background process.
/// </summary>
/// <remarks>
/// This interface is to be extended for each kind of elevated operation we need to perform.
/// Each method will execute an operation recognized by the elevated process from its input arguments.
///
/// The types here need to be projected using CsWinRT, so there are restrictions on them.
/// * The types must be `public sealed class` or `public interface`.
/// * All public members must use only types that can be projected with CsWinRT.
/// If the projection fails, it will only produce a warning, but the build is likely to
/// fail further down the line when attempting to use the types that should have been created.
/// </remarks>
public interface IElevatedComponentOperation
{
    /// <summary>
    /// Writes a string to standard output.
    /// </summary>
    /// <remarks>
    /// This is intended for tests only.
    /// </remarks>
    public void WriteToStdOut(string value);

    /// <summary>
    /// Install a package
    /// </summary>
    /// <param name="packageId">Package id</param>
    /// <param name="catalogName">Package catalog name</param>
    /// <param name="version">Package version</param>
    /// <returns>Install package operation result</returns>
    public IAsyncOperationWithProgress<ElevatedInstallTaskResult, ElevatedInstallTaskProgress> InstallPackageAsync(string packageId, string catalogName, string version, Guid activityId);

    /// <summary>
    /// Create a dev drive
    /// </summary>
    /// <returns>Create dev drive operation hresult code</returns>
    public IAsyncOperation<int> CreateDevDriveAsync();

    /// <summary>
    /// Apply DSC configuration
    /// </summary>
    /// <returns>Apply configuration operation result</returns>
    public IAsyncOperation<ElevatedConfigureTaskResult> ApplyConfigurationAsync(Guid activityId);
}
