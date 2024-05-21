// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.Customization.Models;

public class DevDriveOptimizedMessage : ValueChangedMessage<DevDriveOptimizedData>
{
    public DevDriveOptimizedMessage(DevDriveOptimizedData value)
        : base(value)
    {
    }
}
