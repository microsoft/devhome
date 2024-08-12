// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.Common.Environments.Models;

public class ExtensionsPageLoaded : ValueChangedMessage<bool>
{
    public ExtensionsPageLoaded(bool value)
        : base(value)
    {
    }
}
