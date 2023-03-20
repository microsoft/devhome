// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Factory for objects that run in the elevated background process.
/// </summary>
public interface IElevatedComponentFactory
{
    /// <summary>
    /// Writes a string to standard output.
    /// </summary>
    /// <remarks>
    /// This is intended for tests only.
    /// </remarks>
    public void WriteToStdOut(string value);
}
