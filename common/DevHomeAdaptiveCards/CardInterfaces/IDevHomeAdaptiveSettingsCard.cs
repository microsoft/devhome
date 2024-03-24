// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public interface IDevHomeAdaptiveSettingsCard
{
    public string Description { get; set; }

    public string Header { get; set; }

    public string HeaderIcon { get; set; }

    public IDevHomeAdaptiveSettingsCardAction? ActionElement { get; set; }
}
