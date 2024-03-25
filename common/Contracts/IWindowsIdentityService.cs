// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Contracts;

public interface IWindowsIdentityService
{
    public bool IsUserHyperVAdmin();
}
