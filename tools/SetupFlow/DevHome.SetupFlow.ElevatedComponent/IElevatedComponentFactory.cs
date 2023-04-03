// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ElevatedComponent.AppManagement;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Factory for objects that run in the elevated background process.
/// </summary>
/// <remarks>
/// This interface is to be extended for each kind of elevated operation we need to perform.
/// For each operation, a new Create*() method should be added, which returns an object
/// capable of performing the necessary actions.
///
/// The types here need to be projected using CsWinRT, so there are restrictions on them.
/// * The types must be `public sealed class` or `public interface`.
/// * All public members must use only types that can be projected with CsWinRT.
/// If the projection fails, it will only produce a warning, but the build is likely to
/// fail further down the line when attempting to use the types that should have been created.
/// </remarks>
public interface IElevatedComponentFactory
{
    /// <summary>
    /// Writes a string to standard output.
    /// </summary>
    /// <remarks>
    /// This is intended for tests only.
    /// </remarks>
    public void WriteToStdOut(string value);

    /// <summary>
    /// Creates an object that can be used to install packages from an elevated context.
    /// </summary>
    public PackageInstaller CreatePackageInstaller();
}
