// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKConfigurationUnitWrapper
{
    public SDKConfigurationUnitWrapper(SDK.ConfigurationUnit configurationUnit)
    {
        Type = configurationUnit.Type.Clone() as string;
        Identifier = configurationUnit.Identifier.Clone() as string;
        State = configurationUnit.State;
        IsGroup = configurationUnit.IsGroup;

        if (configurationUnit.Units != null)
        {
            foreach (var unit in configurationUnit.Units)
            {
                UnitWrappers.Add(new SDKConfigurationUnitWrapper(unit));
            }
        }
    }

    // The type of the unit being configured; not a name for this instance.
    public string Type { get; private set; }

    // The identifier name of this instance within the set.
    public string Identifier { get; private set; }

    // The current state of the configuration unit.
    public SDK.ConfigurationUnitState State { get; private set; }

    // Determines if this configuration unit should be treated as a group.
    // A configuration unit group treats its `Settings` as the definition of child units.
    public bool IsGroup { get; private set; }

    // The configuration units that are part of this unit (if IsGroup is true).
    public List<SDK.ConfigurationUnit> Units { get; private set; } = new();

    public List<SDKConfigurationUnitWrapper> UnitWrappers { get; private set; } = new();
}
