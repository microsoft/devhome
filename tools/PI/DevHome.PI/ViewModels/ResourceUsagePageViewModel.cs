// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using LibreHardwareMonitor.Hardware;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.ViewModels;

public partial class ResourceUsagePageViewModel : ObservableObject, IDisposable
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
    private string _cpuUsage = string.Empty;

    [ObservableProperty]
    private string _ramUsage = string.Empty;

    [ObservableProperty]
    private string _diskUsage = string.Empty;

    [ObservableProperty]
    private string _gpuUsage = string.Empty;

    [ObservableProperty]
    private bool _responding;

    [ObservableProperty]
    private ObservableCollection<HWStatusItem> _cpuSensors;

    public ResourceUsagePageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Application.Current.GetService<PerfCounters>().PropertyChanged += PerfCounterHelper_PropertyChanged;

        // Initial population of values
        _cpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().CpuUsage);
        _ramUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().RamUsageInMB);
        _diskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().DiskUsage);
        _gpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().DiskUsage);
        _responding = TargetAppData.Instance.TargetProcess?.Responding ?? false;

        // Application.Current.GetService<HardwareMonitor>().Init();
        // Application.Current.GetService<HardwareMonitor>().PropertyChanged += HWMonitorHelper_PropertyChanged;
        _cpuSensors = new();
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
                    _dispatcher.TryEnqueue(() =>
                    {
                        CpuSensors.Add(new HWStatusItem(sensor));
                    });
                }
            }
        });

        // We don't have a great way to determine when the "Responding" member changes, so we'll poll every 10 seconds using a Timer
        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void TimerCallback(object? state)
    {
        if (CpuSensors.Count > 0)
        {
            _dispatcher.TryEnqueue(() =>
            {
                CpuSensors[0].Update();
            });
        }

        Process? process = TargetAppData.Instance.TargetProcess;

        if (process is not null)
        {
            var newResponding = process.Responding;

            if (newResponding != Responding)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    Responding = newResponding;
                });
            }
        }
    }

    private void HWMonitorHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().CpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.RamUsageInMB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().RamUsageInMB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.DiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().DiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.GpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                GpuUsage = CommonHelper.GetLocalizedString("GpuPerfTextFormatNoLabel", Application.Current.GetService<PerfCounters>().GpuUsage);
            });
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class HardwareTreeViewTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SensorTemplate
    {
        get; set;
    }

    public DataTemplate? HardwareTemplate
    {
        get; set;
    }

    public DataTemplate? SensorGroupTemplate
    {
        get; set;
    }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            SensorNode => SensorTemplate,
            HardwareNode => HardwareTemplate,
            SensorGroupNode => SensorGroupTemplate,
            _ => throw new NotSupportedException(),
        };
    }
}
