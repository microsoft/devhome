// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Environments.Helpers;

public static class ComputeSystemHelpers
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemHelpers));

    public static async Task<byte[]?> GetBitmapImageArrayAsync(ComputeSystemCache computeSystem)
    {
        try
        {
            var result = await computeSystem.GetComputeSystemThumbnailAsync(string.Empty);

            if ((result.Result.Status == ProviderOperationStatus.Failure) || (result.ThumbnailInBytes.Length <= 0))
            {
                _log.Error($"Failed to get thumbnail for compute system {computeSystem}. Error: {result.Result.DiagnosticText}");

                // No thumbnail available, return null so that the card will display the default image.
                return null;
            }

            return result.ThumbnailInBytes;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get thumbnail for compute system {computeSystem}.");
            return null;
        }
    }

    public static BitmapImage? GetBitmapImageFromByteArray(byte[] array)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.SetSource(array.AsBuffer().AsStream().AsRandomAccessStream());
            return bitmap;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get thumbnail from a byte array.");
            return null;
        }
    }

    public static async Task<BitmapImage?> GetBitmapImageAsync(ComputeSystemCache computeSystem)
    {
        var array = await GetBitmapImageArrayAsync(computeSystem);
        return (array != null) ? GetBitmapImageFromByteArray(array) : null;
    }

    public static List<CardProperty> GetComputeSystemCardProperties(IEnumerable<ComputeSystemPropertyCache> properties, string packageFullName)
    {
        var propertyList = new List<CardProperty>();

        try
        {
            foreach (var property in properties)
            {
                propertyList.Add(new CardProperty(property, packageFullName));
            }

            return propertyList;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get all ComputeSystemCardProperties.");
            return propertyList;
        }
    }

    public static async Task<List<CardProperty>> GetComputeSystemCardPropertiesAsync(ComputeSystemCache computeSystem, string packageFullName)
    {
        try
        {
            var curentProperties = await computeSystem.GetComputeSystemPropertiesAsync(string.Empty);
            return GetComputeSystemCardProperties(curentProperties, packageFullName);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to get all properties for compute system {computeSystem}.");
            return new List<CardProperty>();
        }
    }

    public static CardStateColor GetColorBasedOnState(ComputeSystemState state)
    {
        return state switch
        {
            ComputeSystemState.Running => CardStateColor.Success,
            ComputeSystemState.Stopped => CardStateColor.Neutral,
            _ => CardStateColor.Caution,
        };
    }

    public static EnvironmentsCallToActionData UpdateCallToActionText(int providerCount, bool isCreationPage = false)
    {
        var navigateToExtensionsLibrary = false;
        string? callToActionText = null;
        string? callToActionHyperLinkText = null;

        // When the provider count is zero we'll show UX to redirect the user to the extensions library and when it is
        // greater than zero we'll show UX to redirect user to the create environment flow.
        if (providerCount == 0)
        {
            navigateToExtensionsLibrary = true;
            callToActionText = StringResourceHelper.GetResource("NoEnvironmentsAndExtensionsNotInstalledCallToAction");
            callToActionHyperLinkText = StringResourceHelper.GetResource("NoEnvironmentsAndExtensionsNotInstalledButton");
        }
        else if (providerCount > 0 && !isCreationPage)
        {
            // Text to redirect user to Creation flow in Machine configuration
            callToActionText = StringResourceHelper.GetResource("NoEnvironmentsButExtensionsInstalledCallToAction");
            callToActionHyperLinkText = StringResourceHelper.GetResource("NoEnvironmentsButExtensionsInstalledButton");
        }

        return new(navigateToExtensionsLibrary, callToActionText, callToActionHyperLinkText);
    }

    public static void RemoveAllItems<T>(ObservableCollection<T> collection)
    {
        try
        {
            for (var i = collection.Count - 1; i >= 0; i--)
            {
                collection.RemoveAt(i);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unable to remove items from the collection");
        }
    }
}
