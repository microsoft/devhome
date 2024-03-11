// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper interface that can be used to get the WindowsIdentityWrapper.
/// </summary>
public interface IWindowsIdentityService
{
    public WindowsIdentityWrapper GetCurrentWindowsIdentity();
}
