// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.DistributionDefinitions;

namespace WSLExtension.Contracts;

/// <summary>
/// Provides definition information about all the WSL distributions that can be found at
/// <see cref="Constants.KnownDistributionsWebJsonLocation"/>.
/// </summary>
public interface IDistributionDefinitionHelper
{
    /// <summary>
    /// Retrieves a list of objects that contain metadata about WSL distributions that can be
    /// installed from the wsl.exe executable.
    /// </summary>
    /// <returns>
    /// A Dictionary where the key is the name of the distribution and the value is its
    /// <see cref="DistributionDefinition"/> metadata.
    /// </returns>
    public Task<Dictionary<string, DistributionDefinition>> GetDistributionDefinitionsAsync();
}
