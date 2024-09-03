// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.Customization.Models;

public class DevDriveOptimizingMessage : ValueChangedMessage<DevDriveOptimizingData>
{
    public DevDriveOptimizingMessage(DevDriveOptimizingData value)
        : base(value)
    {
    }
}
