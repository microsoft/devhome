// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.PI.Models;

public partial class HardwareNode : ObservableObject, ITreeNode
{
    public HardwareNode(string name)
    {
        _name = name;
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<ITreeNode> _children = new();
}
