// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Security.Principal;
using HyperVExtension.Helpers;
using HyperVExtension.Models;
using Moq;

namespace HyperVExtension.UnitTest.Mocks;

public class WindowsIdentityServiceMock : IWindowsIdentityService
{
    public Mock<WindowsIdentityWrapper> WindowsIdentityWrapperMock { get; set; } = new();

    public IdentityReferenceCollection WindowsIdentityGroups { get; set; } = new();

    public string SecuritySidIdentifier { get; set; } = HyperVStrings.HyperVAdminGroupWellKnownSid;

    public WindowsIdentityServiceMock()
    {
        WindowsIdentityWrapperMock.Setup(x => x.Groups).Returns(WindowsIdentityGroups);
    }

    public WindowsIdentityWrapper GetCurrentWindowsIdentity()
    {
        // Clear the identity collection so tests can update the SecuritySidIdentifier before GetCurrentWindowsIdentity is called.
        WindowsIdentityGroups.Clear();
        if (!string.IsNullOrEmpty(SecuritySidIdentifier))
        {
            WindowsIdentityGroups.Add(new SecurityIdentifier(SecuritySidIdentifier));
        }

        return WindowsIdentityWrapperMock.Object;
    }
}
