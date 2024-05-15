// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the ComputeSystemProperty SDK class that can be used to fetch data once and use is
/// after that without making OOP calls to Compute System extension.
/// </summary>
public class ComputeSystemPropertyCache
{
    public string Name { get; private set; }

    public ComputeSystemPropertyKind PropertyKind { get; private set; }

    public object? Value { get; private set; }

    public Uri Icon { get; private set; }

    public ComputeSystemPropertyCache(ComputeSystemProperty property)
    {
        Name = property.Name;
        PropertyKind = property.PropertyKind;
        Value = ConvertToLocalObjectIfPossible(property.Value);
        Icon = property.Icon;
    }

    private object? ConvertToLocalObjectIfPossible(object value)
    {
        switch (PropertyKind)
        {
            case ComputeSystemPropertyKind.UptimeIn100ns:
                return value as TimeSpan?;
            case ComputeSystemPropertyKind.CpuCount:
            case ComputeSystemPropertyKind.StorageSizeInBytes:
            case ComputeSystemPropertyKind.AssignedMemorySizeInBytes:
                return Convert.ToUInt64(value, CultureInfo.CurrentCulture);
            default:
                return value;
        }
    }
}
