// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace DevHome.PI.Models;

public partial class HWStatusItem : ObservableObject
{
    private readonly ISensor _sensor;
    private readonly string _format;

    public HWStatusItem(ISensor sensor)
    {
        _sensor = sensor;
        _format = "{0}";

        switch (_sensor.SensorType)
        {
            case SensorType.Voltage:
                _format = "{0:F3} V";
                break;
            case SensorType.Current:
                _format = "{0:F3} A";
                break;
            case SensorType.Clock:
                _format = "{0:F1} MHz";
                break;
            case SensorType.Load:
                _format = "{0:F1} %";
                break;
            case SensorType.Temperature:
                _format = "{0:F1} °C";
                break;
            case SensorType.Fan:
                _format = "{0:F0} RPM";
                break;
            case SensorType.Flow:
                _format = "{0:F1} L/h";
                break;
            case SensorType.Control:
                _format = "{0:F1} %";
                break;
            case SensorType.Level:
                _format = "{0:F1} %";
                break;
            case SensorType.Power:
                _format = "{0:F1} W";
                break;
            case SensorType.Data:
                _format = "{0:F1} GB";
                break;
            case SensorType.SmallData:
                _format = "{0:F1} MB";
                break;
            case SensorType.Factor:
                _format = "{0:F3}";
                break;
            case SensorType.Frequency:
                _format = "{0:F1} Hz";
                break;
            case SensorType.Throughput:
                _format = "{0:F1} B/s";
                break;
            case SensorType.TimeSpan:
                _format = "{0:g}";
                break;
            case SensorType.Energy:
                _format = "{0:F0} mWh";
                break;
            case SensorType.Noise:
                _format = "{0:F0} dBA";
                break;
        }
    }

    public string GetName()
    {
        return _sensor.Name;
    }

    public string GetState()
    {
        _sensor.Hardware.Update();
        return ValueToString();
    }

    private string ValueToString()
    {
        if (_sensor != null)
        {
            if (_sensor.Value.HasValue)
            {
                switch (_sensor.SensorType)
                {
                    /*
                    case SensorType.Temperature when _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit:
                        {
                            return $"{value * 1.8 + 32:F1} °F";
                        }*/
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
                                                {
                                                    result = "100Mbps";
                                                    break;
                                                }

                                            case 1000000000:
                                                {
                                                    result = "1Gbps";
                                                    break;
                                                }

                                            default:
                                                {
                                                    if (_sensor.Value < 1024)
                                                    {
                                                        result = $"{_sensor.Value:F0} bps";
                                                    }
                                                    else if (_sensor.Value < 1048576)
                                                    {
                                                        result = $"{_sensor.Value / 1024:F1} Kbps";
                                                    }
                                                    else if (_sensor.Value < 1073741824)
                                                    {
                                                        result = $"{_sensor.Value / 1048576:F1} Mbps";
                                                    }
                                                    else
                                                    {
                                                        result = $"{_sensor.Value / 1073741824:F1} Gbps";
                                                    }
                                                }

                                                break;
                                        }

                                        break;
                                    }

                                default:
                                    {
                                        const int _1MB = 1048576;

                                        result = _sensor.Value < _1MB ? $"{_sensor.Value / 1024:F1} KB/s" : $"{_sensor.Value / _1MB:F1} MB/s";

                                        break;
                                    }
                            }

                            return result;
                        }
#pragma warning disable CA1305
                    case SensorType.TimeSpan:
                        {
                            return _sensor.Value.HasValue ? string.Format(_format, TimeSpan.FromSeconds(_sensor.Value.Value)) : "-";
                        }

                    default:
                        {
                            return string.Format(_format, _sensor.Value);
                        }
#pragma warning restore CA1305
                }
            }
        }

        return "-";
    }
}
