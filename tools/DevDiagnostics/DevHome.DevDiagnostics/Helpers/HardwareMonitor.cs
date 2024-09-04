// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.DevDiagnostics.Models;
using LibreHardwareMonitor.Hardware;

namespace DevHome.DevDiagnostics.Helpers;

public partial class HardwareMonitor : ObservableObject, IDisposable
{
    private readonly Timer _timer;
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsNetworkEnabled = true,
        IsStorageEnabled = true,
    };

    private bool _initialized;

    [ObservableProperty]
    private ObservableCollection<HardwareNode> _hardwares = new();

    public HardwareMonitor()
    {
        ThreadPool.QueueUserWorkItem((o) =>
        {
            _computer.Open();
            _computer.Accept(new UpdateVisitor());
            _initialized = true;
        });

        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
    }

    // Updating Hardware Tree. Should only be called from Dispatcher thread.
    public void UpdateHardwares()
    {
        if (!_initialized)
        {
            return;
        }

        if (Hardwares.Count == 0)
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                Hardwares.Add(CreateHardWareNode(hardware));
            }
        }
        else
        {
            UpdateHardwareData();
            foreach (IHardwareMonitorTreeNode node in Hardwares)
            {
                UpdateSensorData(node);
            }
        }
    }

    // Create a treeview with following structure
    // hardware1 > subhardware > temperature sensor group > sensor1
    //                                                   > sensor2
    //                         > throuput sensor group    > sensor3
    //           > temperature sensor group > sensor4
    // hardware2 > ...
    private HardwareNode CreateHardWareNode(IHardware h)
    {
        HardwareNode hardwareNode = new HardwareNode(h.Name);
        if (h.Sensors.Length > 0)
        {
            Dictionary<SensorType, List<SensorNode>> sensorDict = new Dictionary<SensorType, List<SensorNode>>();

            foreach (ISensor s in h.Sensors)
            {
                if (!sensorDict.TryGetValue(s.SensorType, out var value))
                {
                    List<SensorNode> sensorList = new List<SensorNode>();
                    sensorList.Add(new SensorNode(s));
                    sensorDict[s.SensorType] = sensorList;
                }
                else
                {
                    value.Add(new SensorNode(s));
                }
            }

            foreach (KeyValuePair<SensorType, List<SensorNode>> pair in sensorDict)
            {
                SensorGroupNode sensorGroupNode = new SensorGroupNode(pair.Key, pair.Value);
                hardwareNode.Children.Add(sensorGroupNode);
            }
        }

        foreach (IHardware subhardware in h.SubHardware)
        {
            hardwareNode.Children.Add(CreateHardWareNode(subhardware));
        }

        return hardwareNode;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void TimerCallback(object? state)
    {
        if (Hardwares.Count < 0)
        {
            UpdateHardwareData();
            foreach (IHardwareMonitorTreeNode node in Hardwares)
            {
                UpdateSensorData(node);
            }
        }
    }

    public void UpdateHardwareData()
    {
        foreach (IHardware hardware in _computer.Hardware)
        {
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                subhardware.Update();
            }

            hardware.Update();
        }
    }

    public void UpdateSensorData(IHardwareMonitorTreeNode node)
    {
        if (node is SensorNode sensorNode)
        {
            sensorNode.Update();
        }
        else if (node is SensorGroupNode sensorGroupNode)
        {
            foreach (IHardwareMonitorTreeNode child in sensorGroupNode.Children)
            {
                UpdateSensorData(child);
            }
        }
        else if (node is HardwareNode hardwareNode)
        {
            foreach (IHardwareMonitorTreeNode child in hardwareNode.Children)
            {
                UpdateSensorData(child);
            }
        }
    }
}

public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}
