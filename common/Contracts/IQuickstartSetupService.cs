// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace DevHome.Common.Contracts;

public interface IQuickstartSetupService
{
    public bool IsDevHomeAzureExtensionInstalled();

    public Task InstallDevHomeAzureExtensionAsync();
}
