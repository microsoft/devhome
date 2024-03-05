// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Principal;

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper class for the WindowsIdentity class.
/// </summary>
public class WindowsIdentityWrapper
{
    private readonly WindowsIdentity _windowsIdentity = WindowsIdentity.GetCurrent();

    // Get the sid's of the current user.
    public virtual IdentityReferenceCollection Groups => _windowsIdentity.Groups!;
}
