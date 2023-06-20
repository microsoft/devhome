// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace DevHome.SetupFlow.Services;
public interface IAppManagementInitializer
{
    public Task InitializeAsync();
}
