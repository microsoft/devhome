// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.PI.Helpers;
using LibreHardwareMonitor.Hardware;

namespace DevHome.PI.Models;

public partial class HardwareMonitor
{
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

    private readonly List<HWStatusItem> _items = new();

    public HardwareMonitor()
    {
    }

    public void Init()
    {
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        GetHardwareList();
        ISensor? sensor1 = FindSensor(@"/intelcpu/0/load/0");
        if (sensor1 != null)
        {
            _items.Add(new HWStatusItem(sensor1));
        }

        ISensor? sensor2 = FindSensor(@"/ram/data/0");
        if (sensor2 != null)
        {
            _items.Add(new HWStatusItem(sensor2));
        }

        ISensor? sensor3 = FindSensor(@"/ram/data/2");
        if (sensor3 != null)
        {
            _items.Add(new HWStatusItem(sensor3));
        }
    }

    public IList<IHardware> GetHardwareList()
    {
        Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
        };

        computer.Open();
        computer.Accept(new UpdateVisitor());

        return computer.Hardware;
    }

    public ISensor? FindSensor(string name)
    {
        foreach (IHardware hardware in _computer.Hardware)
        {
            foreach (IHardware subhardware in hardware.SubHardware)
            {
                foreach (ISensor sensor in subhardware.Sensors)
                {
                    if (sensor.Identifier.ToString() == name)
                    {
                        return sensor;
                    }
                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                if (sensor.Identifier.ToString() == name)
                {
                    return sensor;
                }
            }
        }

        return null;
    }
}
