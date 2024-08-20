// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace DevHome.Dashboard.Services;

internal interface IWidgetExtensionService
{
    /// <summary>
    /// Gets whether the given providerDefinitionId represents a CoreWidgetProvider of any build ring
    /// </summary>
    /// <returns>True if the given providerDefinitionId represents a CoreWidgetProvider, otherwise false.</returns>
    bool IsCoreWidgetProvider(string providerDefinitionId);

    Task EnsureCoreWidgetExtensionStarted(string providerDefinitionId);
}
