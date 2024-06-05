// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.Customization.Models;

public class DevDriveTrustedMessage : ValueChangedMessage<DevDriveTrustedData>
{
    public DevDriveTrustedMessage(DevDriveTrustedData value)
        : base(value)
    {
    }
}
