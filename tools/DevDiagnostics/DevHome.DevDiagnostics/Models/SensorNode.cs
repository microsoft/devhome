// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.DevDiagnostics.Helpers;
using LibreHardwareMonitor.Hardware;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.AI.MachineLearning;

namespace DevHome.DevDiagnostics.Models;

public partial class SensorNode : ObservableObject, IHardwareMonitorTreeNode
{
    private readonly ISensor _sensor;
    private readonly string _unitFormat;

    public SensorNode(ISensor s)
    {
        _name = s.Name;
        _value = GetValueString();
        _sensor = s;

        // Set the default format to "{0}" if no specific format is found
        _unitFormat = "{0}";

        switch (_sensor.SensorType)
        {
            case SensorType.Voltage:
                _unitFormat = "VoltageSensorFormat";
                break;
            case SensorType.Current:
                _unitFormat = "CurrentSensorFormat";
                break;
            case SensorType.Clock:
                _unitFormat = "ClockSensorFormat";
                break;
            case SensorType.Load:
                _unitFormat = "LoadSensorFormat";
                break;
            case SensorType.Temperature:
                _unitFormat = "TemperatureSensorFormat";
                break;
            case SensorType.Fan:
                _unitFormat = "FanSensorFormat";
                break;
            case SensorType.Flow:
                _unitFormat = "FlowSensorFormat";
                break;
            case SensorType.Control:
                _unitFormat = "ControlSensorFormat";
                break;
            case SensorType.Level:
                _unitFormat = "LevelSensorFormat";
                break;
            case SensorType.Power:
                _unitFormat = "PowerSensorFormat";
                break;
            case SensorType.Data:
                _unitFormat = "DataSensorFormat";
                break;
            case SensorType.SmallData:
                _unitFormat = "SmallDataSensorFormat";
                break;
            case SensorType.Factor:
                _unitFormat = "FactorSensorFormat";
                break;
            case SensorType.Frequency:
                _unitFormat = "FrequencySensorFormat";
                break;
            case SensorType.Throughput:
                _unitFormat = "ThroughputSensorFormat";
                break;
            case SensorType.TimeSpan:
                _unitFormat = "TimeSpanSensorFormat";
                break;
            case SensorType.Energy:
                _unitFormat = "EnergySensorFormat";
                break;
            case SensorType.Noise:
                _unitFormat = "NoiseSensorFormat";
                break;
        }
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _value;

    public void Update()
    {
        Value = GetValueString();
    }

    private string GetValueString()
    {
        if (_sensor != null && _sensor.Value.HasValue && !float.IsNaN((float)_sensor.Value))
        {
            switch (_sensor.SensorType)
            {
                case SensorType.Throughput:
                    {
                        string result;
                        switch (_sensor.Name)
                        {
                            case "Connection Speed":
                                {
                                    switch (_sensor.Value)
                                    {
                                        case 100000000:
                                            result = CommonHelper.GetLocalizedString("ConnectionSpeedSensorValue100Mbps");
                                            break;
                                        case 1000000000:
                                            result = CommonHelper.GetLocalizedString("ConnectionSpeedSensorValue1Gbps");
                                            break;
                                        default:
                                            if (_sensor.Value < 1024)
                                            {
                                                result = CommonHelper.GetLocalizedString("ConnectionSpeedSensorFormatBps", _sensor.Value);
                                            }
                                            else if (_sensor.Value < 1048576)
                                            {
                                                result = CommonHelper.GetLocalizedString("ConnectionSpeedSensorFormatKbps", _sensor.Value / 1024);
                                            }
                                            else if (_sensor.Value < 1073741824)
                                            {
                                                result = CommonHelper.GetLocalizedString("ConnectionSpeedSensorFormatMbps", _sensor.Value / 1048576);
                                            }
                                            else
                                            {
                                                result = CommonHelper.GetLocalizedString("ConnectionSpeedSensorFormatGbps", _sensor.Value / 1073741824);
                                            }

                                            break;
                                    }

                                    break;
                                }

                            default:
                                {
                                    const int _1MB = 1048576;
                                    result = _sensor.Value < _1MB ? CommonHelper.GetLocalizedString("ConnectionSpeedSensorFormatKbps", _sensor.Value / 1024) : CommonHelper.GetLocalizedString("ConnectionSpeedSensorFormatMbps", _sensor.Value / _1MB);
                                    break;
                                }
                        }

                        return result;
                    }

                case SensorType.TimeSpan:
                    {
                        return CommonHelper.GetLocalizedString(_unitFormat, TimeSpan.FromSeconds(_sensor.Value.Value));
                    }

                default:
                    {
                        return CommonHelper.GetLocalizedString(_unitFormat, _sensor.Value);
                    }
            }
        }

        return string.Empty;
    }
}

public interface IHardwareMonitorTreeNode
{
    string Name
    {
        get; set;
    }
}

public partial class HardwareNode : ObservableObject, IHardwareMonitorTreeNode
{
    public HardwareNode(string name)
    {
        _name = name;
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<IHardwareMonitorTreeNode> _children = new();
}

public partial class SensorGroupNode : ObservableObject, IHardwareMonitorTreeNode
{
    public SensorGroupNode(SensorType type, List<SensorNode> sensors)
    {
        _children = new ObservableCollection<SensorNode>(sensors);

        switch (type)
        {
            case SensorType.Voltage:
                _name = CommonHelper.GetLocalizedString("SensorTypeVoltage");
                break;
            case SensorType.Current:
                _name = CommonHelper.GetLocalizedString("SensorTypeCurrent");
                break;
            case SensorType.Clock:
                _name = CommonHelper.GetLocalizedString("SensorTypeClock");
                break;
            case SensorType.Load:
                _name = CommonHelper.GetLocalizedString("SensorTypeLoad");
                break;
            case SensorType.Temperature:
                _name = CommonHelper.GetLocalizedString("SensorTypeTemperature");
                break;
            case SensorType.Fan:
                _name = CommonHelper.GetLocalizedString("SensorTypeFan");
                break;
            case SensorType.Flow:
                _name = CommonHelper.GetLocalizedString("SensorTypeFlow");
                break;
            case SensorType.Control:
                _name = CommonHelper.GetLocalizedString("SensorTypeControl");
                break;
            case SensorType.Level:
                _name = CommonHelper.GetLocalizedString("SensorTypeLevel");
                break;
            case SensorType.Power:
                _name = CommonHelper.GetLocalizedString("SensorTypePower");
                break;
            case SensorType.Data:
            case SensorType.SmallData:
                _name = CommonHelper.GetLocalizedString("SensorTypeData");
                break;
            case SensorType.Factor:
                _name = CommonHelper.GetLocalizedString("SensorTypeFactor");
                break;
            case SensorType.Frequency:
                _name = CommonHelper.GetLocalizedString("SensorTypeFrequency");
                break;
            case SensorType.Throughput:
                _name = CommonHelper.GetLocalizedString("SensorTypeThroughput");
                break;
            case SensorType.TimeSpan:
                _name = CommonHelper.GetLocalizedString("SensorTypeTimeSpan");
                break;
            case SensorType.Energy:
                _name = CommonHelper.GetLocalizedString("SensorTypeEnergy");
                break;
            case SensorType.Noise:
                _name = CommonHelper.GetLocalizedString("SensorTypeNoise");
                break;
            default:
                _name = type.ToString();
                break;
        }
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<SensorNode> _children;
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
