// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper interface that can be used to get the WindowsIdentityWrapper.
/// </summary>
public interface IWindowsIdentityService
{
    public WindowsIdentityWrapper GetCurrentWindowsIdentity();
}
