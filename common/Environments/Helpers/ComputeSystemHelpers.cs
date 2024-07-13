// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

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
                Log.Logger()?.ReportError($"Failed to get thumbnail for compute system {computeSystemWrapper}. Error: {result.Result.DiagnosticText}");

                // No thumbnail available, return null so that the card will display the default image.
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.SetSource(result.ThumbnailInBytes.AsBuffer().AsStream().AsRandomAccessStream());
            return bitmap;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Failed to get thumbnail for compute system {computeSystemWrapper}.", ex);
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
            Log.Logger()?.ReportError($"Failed to get all properties for compute system {computeSystemWrapper}.", ex);
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
}
