// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    public static async Task<BitmapImage?> GetBitmapImageAsync(ComputeSystem computeSystemWrapper)
    {
        try
        {
            var result = await computeSystemWrapper.GetComputeSystemThumbnailAsync(string.Empty);

            if ((result.Result.Status == ProviderOperationStatus.Failure) || (result.ThumbnailInBytes.Length <= 0))
            {
                Log.Error($"Failed to get thumbnail for compute system {computeSystemWrapper}. Error: {result.Result.DiagnosticText}");

                // No thumbnail available, return null so that the card will display the default image.
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.SetSource(result.ThumbnailInBytes.AsBuffer().AsStream().AsRandomAccessStream());
            return bitmap;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to get thumbnail for compute system {computeSystemWrapper}.");
            return null;
        }
    }

    public static async Task<List<CardProperty>> GetComputeSystemPropertiesAsync(ComputeSystem computeSystemWrapper, string packageFullName)
    {
        var propertyList = new List<CardProperty>();

        try
        {
            var cuurentProperties = await computeSystemWrapper.GetComputeSystemPropertiesAsync(string.Empty);
            foreach (var property in cuurentProperties)
            {
                propertyList.Add(new CardProperty(property, packageFullName));
            }

            return propertyList;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to get all properties for compute system {computeSystemWrapper}.");
            return propertyList;
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

    public static (bool, string?, string?) UpdateCallToActionText(int providerCount, bool isCreationPage = false)
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

        return (navigateToExtensionsLibrary, callToActionText, callToActionHyperLinkText);
    }
}
