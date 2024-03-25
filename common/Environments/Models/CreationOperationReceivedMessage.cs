// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevHome.Common.Environments.Models;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationOperationReceivedMessage : ValueChangedMessage<CreateComputeSystemOperation>
{
    public CreationOperationReceivedMessage(CreateComputeSystemOperation value)
        : base(value)
    {
    }
}
