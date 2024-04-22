// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Contracts;

public interface IWindowsIdentityService
{
    string? GetCurrentUserName();

    bool IsUserHyperVAdmin();
}
