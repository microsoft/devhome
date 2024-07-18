// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Contracts;

namespace DevHome.Common.Services;

/// <summary>
/// From the Hyper-V extension.
/// Checks if the current user is part of the Hyper-V Admin group.
/// </summary>
public class WindowsIdentityService : IWindowsIdentityService
{
    private readonly WindowsIdentity _currentUserIdentity = WindowsIdentity.GetCurrent();

    // From: https://learn.microsoft.com/windows-server/identity/ad-ds/manage/understand-security-identifiers
    private const string HyperVAdminSid = "S-1-5-32-578";

    public bool IsUserHyperVAdmin()
    {
        var wasHyperVSidFound = _currentUserIdentity?.Groups?.Any(sid => sid.Value == HyperVAdminSid);
        return wasHyperVSidFound ?? false;
    }
}
