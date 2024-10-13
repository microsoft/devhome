// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
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
            var currentProperties = await computeSystem.GetComputeSystemPropertiesAsync(string.Empty);

            // Remove properties with empty values
            var filteredProperties = currentProperties
                .Where(property => property?.Value != null && !string.IsNullOrEmpty(property.Value.ToString()))
                .ToList();

            return GetComputeSystemCardProperties(filteredProperties, packageFullName);
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

    public static EnvironmentsCallToActionData UpdateCallToActionText()
    {
        // Text to redirect user to Creation flow in Machine configuration
        var callToActionText = StringResourceHelper.GetResource("NoEnvironmentsButExtensionsInstalledCallToAction");
        var callToActionHyperLinkText = StringResourceHelper.GetResource("NoEnvironmentsButExtensionsInstalledButton");

        return new(callToActionText, callToActionHyperLinkText);
    }

    /// <summary>
    /// Safely removes all items from an observable collection and replaces them with new items.
    /// </summary>
    /// <remarks>
    /// There can be random COM exceptions due to using the "Clear()" method in an observable collection. This method
    /// is used so that we can safely clear the observable collection without throwing an exceptions. This is related
    /// to this GitHub issue https://github.com/microsoft/microsoft-ui-xaml/issues/8684. To work around this,
    /// this method is used to remove all items individually from the end of the collection to the beginning of the collection.
    /// </remarks>
    /// <typeparam name="T">Type of objects that the collection contains</typeparam>
    /// <param name="collectionToUpdate">An observable collection that contains zero to N elements that will have its contents replaced</param>
    /// <param name="listWithUpdates">A list that contains zero to N elements whose elements will be added to collectionToUpdate</param>
    /// <returns>
    /// True only if we successfully replaced all items in the collection. False otherwise.
    /// </returns>
    public static bool RemoveAllItemsAndReplace<T>(ObservableCollection<T> collectionToUpdate, List<T> listWithUpdates)
    {
        try
        {
            for (var i = collectionToUpdate.Count - 1; i >= 0; i--)
            {
                collectionToUpdate.RemoveAt(i);
            }

            for (var i = 0; i < listWithUpdates.Count; i++)
            {
                collectionToUpdate.Add(listWithUpdates[i]);
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unable to remove items from the collection");
        }

        return false;
    }

    public static (string DisplayMessage, string DiagnosticText, EnvironmentsTelemetryStatus Status) LogResult(ProviderOperationResult? result, ILogger logger)
    {
        var telemetryStatus = EnvironmentsTelemetryStatus.Succeeded;

        if (result == null)
        {
            var logErrorMsg = $"The returned result object was null";
            logger.Error(logErrorMsg);
            return (logErrorMsg, logErrorMsg, EnvironmentsTelemetryStatus.Failed);
        }
        else if (result.Status == ProviderOperationStatus.Failure)
        {
            logger.Error(result.ExtendedError, $"Operation failed with error:{result.DiagnosticText}");
            telemetryStatus = EnvironmentsTelemetryStatus.Failed;
        }

        return (result.DisplayMessage, result.DiagnosticText, telemetryStatus);
    }
}
