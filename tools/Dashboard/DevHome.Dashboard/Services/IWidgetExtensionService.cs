// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace DevHome.Dashboard.Services;

internal interface IWidgetExtensionService
{
    bool IsCoreWidgetExtension(string providerDefinitionId);

    Task EnsureCoreWidgetExtensionStarted(string providerDefinitionId);
}
