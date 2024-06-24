// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Principal;

namespace DevHome.Common.Helpers;

/// <summary>
/// Wrapper to interact with the WindowsIdentity class
/// </summary>
public class WindowsIdentityHelper
{
    private readonly WindowsIdentity _currentUserIdentity = WindowsIdentity.GetCurrent();

    // From: https://learn.microsoft.com/windows-server/identity/ad-ds/manage/understand-security-identifiers
    private const string HyperVAdminSid = "S-1-5-32-578";

    public virtual bool IsUserHyperVAdmin()
    {
        var wasHyperVSidFound = _currentUserIdentity?.Groups?.Any(sid => sid.Value == HyperVAdminSid);
        return wasHyperVSidFound ?? false;
    }

    public virtual string? GetCurrentUserName()
    {
        return _currentUserIdentity?.Name;
    }

    // Returns true if the current user has the built-in Administrators claim indicating that
    // they could elevate to an administrator role via UAC if needed. Does not check if the process
    // is running elevated.
    public virtual bool IsUserAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Value;
        return identity.Claims.Any(c => c.Value == adminSid);
    }
}
