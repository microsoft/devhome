// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Newtonsoft.Json.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Enum value that represent additional non compute system specific actions that can be taken by
/// the user from Dev Home.
/// </summary>
public enum EnvironmentAdditionalActions
{
    PinToStart,
    PinToTaskBar,
}

/// <summary>
/// Enum values that are used to visually represent the state of a compute system in the UI.
/// </summary>
public enum CardStateColor
{
    Success,
    Neutral,
    Caution,
}

public partial class CardProperty : ObservableObject
{
    private const int MaxBufferLength = 1024;

    [ObservableProperty]
    private BitmapImage? _icon;

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private object? _value;

    public string PackageFullName { get; private set; }

    public CardProperty(ComputeSystemProperty property, string packageFullName)
    {
        Title = property.Name;
        PackageFullName = packageFullName;
        UpdateTitleBasedOnPropertyKind(property.Name, property.PropertyKind);
        UpdateValueBasedOnPropertyKind(property.Value, property.PropertyKind);

        if (property.Icon != null)
        {
            Icon = ConvertMsResourceToIcon(property.Icon, packageFullName);
        }
    }

    public void UpdateValueBasedOnPropertyKind(object? value, ComputeSystemPropertyKind propertyKind)
    {
        switch (propertyKind)
        {
            case ComputeSystemPropertyKind.AssignedMemorySizeInBytes:
            case ComputeSystemPropertyKind.StorageSizeInBytes:
                Value = ConvertBytesToString(value) ?? "-";
                break;
            case ComputeSystemPropertyKind.UptimeIn100ns:
                Value = Convert100nsToString(value as TimeSpan?) ?? "-";
                break;

            // for generic and cpu count cases.
            default:
                Value = ConvertObjectToString(value) ?? "-";
                break;
        }
    }

    public void UpdateTitleBasedOnPropertyKind(string title, ComputeSystemPropertyKind propertyKind)
    {
        switch (propertyKind)
        {
            case ComputeSystemPropertyKind.CpuCount:
                Title = StringResourceHelper.GetResource("ComputeSystemCpu");
                break;
            case ComputeSystemPropertyKind.AssignedMemorySizeInBytes:
                Title = StringResourceHelper.GetResource("ComputeSystemAssignedMemory");
                break;
            case ComputeSystemPropertyKind.StorageSizeInBytes:
                Title = StringResourceHelper.GetResource("ComputeSystemStorage");
                break;
            case ComputeSystemPropertyKind.UptimeIn100ns:
                Title = StringResourceHelper.GetResource("ComputeSystemUptime");
                break;

            // for generic
            default:
                Title = string.IsNullOrEmpty(title) ? StringResourceHelper.GetResource("ComputeSystemUnknownWithColon") : title + ":";
                break;
        }
    }

    /// <summary>
    /// Converts a passed in ms-resource URI and package full name to a BitmapImage.
    /// </summary>
    /// <param name="iconPathUri">the ms-resource:// path to an image resource in an app packages pri file.</param>
    /// <returns>The bitmap image that represents the icon.</returns>
    public static unsafe BitmapImage ConvertMsResourceToIcon(Uri iconPathUri, string packageFullName)
    {
        try
        {
            var indirectPathToResource = "@{" + packageFullName + "? " + iconPathUri.AbsoluteUri + "}";
            Span<char> outputBuffer = new char[MaxBufferLength];

            fixed (char* outBufferPointer = outputBuffer)
            {
                fixed (char* resourcePathPointer = indirectPathToResource)
                {
                    var res = PInvoke.SHLoadIndirectString(resourcePathPointer, new PWSTR(outBufferPointer), (uint)outputBuffer.Length, null);
                    if (res.Succeeded)
                    {
                        var iconImageLocation = new string(outputBuffer.TrimEnd('\0'));

                        if (File.Exists(iconImageLocation))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.UriSource = new Uri(iconImageLocation);
                            return bitmap;
                        }
                    }

                    Log.Logger()?.ReportError($"Failed to find icon image in path: {iconPathUri} for package: {packageFullName} due to error: 0x{res.Value:X}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Failed to load icon from ms-resource: {iconPathUri} for package: {packageFullName} due to error:", ex);
        }

        return new BitmapImage();
    }

    public string? Convert100nsToString(TimeSpan? timeSpan)
    {
        if (timeSpan == null)
        {
            return null;
        }

        return timeSpan.Value.ToString("g", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Convert bytes to localized string.
    /// </summary>
    /// <param name="size">value in bytes/></param>
    /// <returns>Localized string in Mb, Gb or Tb.</returns>
    public string ConvertBytesToString(object? size)
    {
        try
        {
            if (size == null)
            {
                return string.Empty;
            }

            var sizeInBytes = Convert.ToUInt64(size, CultureInfo.CurrentCulture);

            unsafe
            {
                // 15 characters + null terminator.
                var buffer = new string(' ', 16);
                fixed (char* tempPath = buffer)
                {
                    var result =
                        PInvoke.StrFormatByteSizeEx(
                            sizeInBytes,
                            SFBS_FLAGS.SFBS_FLAGS_TRUNCATE_UNDISPLAYED_DECIMAL_DIGITS,
                            tempPath,
                            PInvoke.MAX_PATH);
                    if (result != 0)
                    {
                        // fallback to using community toolkit which shows this unlocalized. In the form of 50 GB, 40 TB etc.
                        return CommunityToolkit.Common.Converters.ToFileSizeString((long)sizeInBytes);
                    }
                    else
                    {
                        return buffer.Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Failed to convert size in bytes to ulong. Error: {ex}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Attempt to find an object's type and convert it to that type and then to a string.
    /// Only a few types are supported. More can be added as needed.
    /// </summary>
    /// <param name="value">value returned by an extension</param>
    public string ConvertObjectToString(object? value)
    {
        if (value == null)
        {
            return "-";
        }

        var type = value.GetType();
        if (type == typeof(string))
        {
            return value.ToString() ?? "-";
        }
        else if (type == typeof(int))
        {
            return ((int)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(uint))
        {
            return ((uint)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(long))
        {
            return ((long)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(ulong))
        {
            return ((ulong)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(short))
        {
            return ((short)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(ushort))
        {
            return ((ushort)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(byte))
        {
            return ((byte)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(sbyte))
        {
            return ((sbyte)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(float))
        {
            return ((float)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(double))
        {
            return ((double)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(decimal))
        {
            return ((decimal)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(bool))
        {
            return ((bool)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(DateTime))
        {
            return ((DateTime)value).ToString(CultureInfo.CurrentCulture);
        }
        else if (type == typeof(TimeSpan))
        {
            return ((TimeSpan)value).ToString("g", CultureInfo.CurrentCulture);
        }
        else if (type == typeof(Guid))
        {
            return ((Guid)value).ToString();
        }

        return "-";
    }
}
