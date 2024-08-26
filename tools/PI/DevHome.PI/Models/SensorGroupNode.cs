// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.PI.Models;

public partial class SensorGroupNode : ObservableObject, ITreeNode
{
    public SensorGroupNode(string name, List<SensorNode> sensors)
    {
        _name = name;
        _children = new ObservableCollection<SensorNode>(sensors);
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<SensorNode> _children = new();
}
