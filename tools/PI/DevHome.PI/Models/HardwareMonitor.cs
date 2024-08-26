// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using LibreHardwareMonitor.Hardware;

namespace DevHome.PI.Models;

// TODO:
// hardware added and removed handler (computer.HardwareAdded)
// user choice: sensor value update interval, sensor value time interval, celcius vs f,
// option to hide hardware? change min, max value column
// power mode changed -> battery status?
// plot?
public partial class HardwareMonitor : ObservableObject
{
    private readonly Timer _timer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
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

    [ObservableProperty]
    private ObservableCollection<HardwareNode> hardwares = new();

    // [ObservableProperty]
    // private ISensor _cpuUsage;
    public HardwareMonitor()
    {
        _cpuSensors = new();
    }

    public void Init()
    {
        ThreadPool.QueueUserWorkItem((o) =>
        {
            _computer.Open();
            _computer.Accept(new UpdateVisitor());

            List<IHardware> motherboards = new();

            List<IHardware> superIO = new();

            List<IHardware> memory = new();
            List<IHardware> gpu = new();
            List<IHardware> cpu = new();
            List<IHardware> storage = new();
            List<IHardware> network = new();
            List<IHardware> cooler = new();
            List<IHardware> embeddedcontroller = new();
            List<IHardware> psu = new();
            List<IHardware> battery = new();

            foreach (IHardware hardware in _computer.Hardware)
            {
                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    switch (subhardware.HardwareType)
                    {
                        case HardwareType.Motherboard:
                            motherboards.Add(subhardware);
                            break;
                        case HardwareType.SuperIO:
                            superIO.Add(subhardware);
                            break;
                        case HardwareType.Cpu:
                            cpu.Add(subhardware);
                            break;
                        case HardwareType.Memory:
                            memory.Add(subhardware);
                            break;
                        case HardwareType.GpuAmd:
                        case HardwareType.GpuNvidia:
                        case HardwareType.GpuIntel:
                            gpu.Add(subhardware);
                            break;
                        case HardwareType.Storage:
                            storage.Add(subhardware);
                            break;
                        case HardwareType.Network:
                            network.Add(subhardware);
                            break;
                        case HardwareType.Cooler:
                            cooler.Add(subhardware);
                            break;
                        case HardwareType.EmbeddedController:
                            embeddedcontroller.Add(subhardware);
                            break;
                        case HardwareType.Psu:
                            psu.Add(subhardware);
                            break;
                        case HardwareType.Battery:
                            battery.Add(subhardware);
                            break;
                    }
                }

                switch (hardware.HardwareType)
                {
                    case HardwareType.Motherboard:
                        motherboards.Add(hardware);
                        break;
                    case HardwareType.SuperIO:
                        superIO.Add(hardware);
                        break;
                    case HardwareType.Cpu:
                        cpu.Add(hardware);
                        break;
                    case HardwareType.Memory:
                        memory.Add(hardware);
                        break;
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuIntel:
                        gpu.Add(hardware);
                        break;
                    case HardwareType.Storage:
                        storage.Add(hardware);
                        break;
                    case HardwareType.Network:
                        network.Add(hardware);
                        break;
                    case HardwareType.Cooler:
                        cooler.Add(hardware);
                        break;
                    case HardwareType.EmbeddedController:
                        embeddedcontroller.Add(hardware);
                        break;
                    case HardwareType.Psu:
                        psu.Add(hardware);
                        break;
                    case HardwareType.Battery:
                        battery.Add(hardware);
                        break;
                }
            }

            foreach (ISensor sensor in cpu[0].Sensors)
            {
                if (sensor.SensorType == SensorType.Load)
                {
                    CpuSensors.Add(new HWStatusItem(sensor));
                }
            }

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
        });
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
