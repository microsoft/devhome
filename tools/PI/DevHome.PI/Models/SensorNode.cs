// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace DevHome.PI.Models;

public partial class SensorNode : ObservableObject, ITreeNode
{
    public SensorNode(ISensor s)
    {
        _name = s.Name;
        _value = s.Value;
        _sensor = s;
    }

    private readonly ISensor _sensor;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private float? _value;

    public void Update()
    {
        Value = _sensor.Value;
    }
}
