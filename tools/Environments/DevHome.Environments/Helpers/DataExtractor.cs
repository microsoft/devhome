// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Animations;
using DevHome.Common.Environments.Models;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DevHome.Environments.Helpers;

public class DataExtractor
{
    /// <summary>
    /// Converts the compute computeSystem thumbnail (byte array) to a BitmapImage.
    /// </summary>
    /// <param name="computeSystem"></param>
    public static BitmapImage GetCardBodyImage(IComputeSystem computeSystem)
    {
        var bitmap = new BitmapImage();
        var result = computeSystem.GetComputeSystemThumbnailAsync(string.Empty).GetAwaiter().GetResult();
        if (result.Result.Status == ProviderOperationStatus.Success)
        {
            var thumbnail = result.ThumbnailInBytes;
            if (thumbnail.Length > 0)
            {
                bitmap.SetSource(thumbnail.AsBuffer().AsStream().AsRandomAccessStream());
            }

            return bitmap;
        }
        else
        {
            // ToDo: Remove this test value
            return new BitmapImage { UriSource = new Uri("ms-appx:///Assets/Temp-Bloom.jpg"), };
        }
    }

    // ToDo: Use resources instead of literals
    // ToDo: Add a pause after each operation
    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the dot button.
    /// </summary>
    /// <param name="computeSystem"></param>
    public static List<OperationsViewModel> FillDotButtonOperations(IComputeSystem computeSystem)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Restart))
        {
            operations.Add(new OperationsViewModel("Restart", "\uE777", computeSystem.RestartAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Delete))
        {
            operations.Add(new OperationsViewModel("Delete", "\uE74D", computeSystem.DeleteAsync));
        }

        // ToDo: Correct the function used
        // operations.Add(new OperationsViewModel("Pin To Taskbar", "\uE718", computeSystem.DeleteAsync));
        // operations.Add(new OperationsViewModel("Add to Start Menu", "\uF0DF", computeSystem.DeleteAsync));
        return operations;
    }

    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the launch button.
    /// </summary>
    /// <param name="computeSystem"></param>
    public static List<OperationsViewModel> FillLaunchButtonOperations(IComputeSystem computeSystem)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Start))
        {
            operations.Add(new OperationsViewModel("Start", "\uE768", computeSystem.StartAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.ShutDown))
        {
            operations.Add(new OperationsViewModel("Stop", "\uE71A", computeSystem.TerminateAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.CreateSnapshot))
        {
            operations.Add(new OperationsViewModel("Checkpoint", "\uE7C1", computeSystem.CreateSnapshotAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.RevertSnapshot))
        {
            operations.Add(new OperationsViewModel("Revert", "\uE7A7", computeSystem.RevertSnapshotAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Pause))
        {
            operations.Add(new OperationsViewModel("Pause", "\uE769", computeSystem.PauseAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Resume))
        {
            operations.Add(new OperationsViewModel("Resume", "\uF2C6", computeSystem.ResumeAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Terminate))
        {
            operations.Add(new OperationsViewModel("Terminate", "\uE71A", computeSystem.TerminateAsync));
        }

        return operations;
    }

    /// <summary>
    /// Checks for properties and adds the text, value, and icon associated with the property.
    /// Only handles the default 4 cases for now; no custom icons
    /// TODO: remove this in favor of using the shared CardProperty class as this doesn't handle
    /// buffer overflows, and other data types other than ints for the value propery.
    /// </summary>
    /// <param name="computeSystem"></param>
    public static List<PropertyViewModel> FillPropertiesAsync(IComputeSystem computeSystem)
    {
        var result = new List<PropertyViewModel>();
        var properties = computeSystem.GetComputeSystemPropertiesAsync(string.Empty).GetAwaiter().GetResult();
        foreach (var property in properties)
        {
            var value = property.Value;
            if (value.GetType() == typeof(string))
            {
                continue;
            }

            // Temporarily wrap in try-catch to handle the case where the value is not an int.
            // This will be removed when the CardProperty class is used.
            try
            {
                switch (property.PropertyKind)
                {
                    case ComputeSystemPropertyKind.CpuCount:
                        result.Add(new PropertyViewModel("vCPU", (int)value, "\uEEA1"));
                        break;

                    case ComputeSystemPropertyKind.AssignedMemorySizeInBytes:
                        int memory = (int)((long)value / (1024 * 1024 * 1024));
                        result.Add(new PropertyViewModel("GB RAM", memory, "\uEEA0"));
                        break;

                    case ComputeSystemPropertyKind.StorageSizeInBytes:
                        int storage = (int)((long)value / (1024 * 1024 * 1024));
                        result.Add(new PropertyViewModel("GB Storage", storage, "\uEDA2"));
                        break;

                    // TODO: update this to use the new CardProperty class. Uptime is a TimeSpan, not an int.
                    // case ComputeSystemPropertyKind.UptimeIn100ns:
                    //   result.Add(new PropertyViewModel("Uptime", (int)value / 100, "\uE703"));
                    //    break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        return result;
    }
}
