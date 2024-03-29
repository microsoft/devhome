// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DevHome.SetupFlow.Models.Environments;

public class RemoveAdaptiveCardPanelMessage : ValueChangedMessage<bool>
{
    public RemoveAdaptiveCardPanelMessage(bool value)
        : base(value)
    {
    }
}
