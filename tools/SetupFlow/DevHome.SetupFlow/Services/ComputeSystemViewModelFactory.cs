// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Factory class for creating <see cref="ComputeSystemCardViewModel"/> instances asynchronously.
/// </summary>
public class ComputeSystemViewModelFactory
{
    public async Task<ComputeSystemCardViewModel> CreateCardViewModelAsync(ComputeSystem computeSystem)
    {
        var cardViewModel = new ComputeSystemCardViewModel();

        try
        {
            cardViewModel.ComputeSystemWrapper = computeSystem;
            cardViewModel.ComputeSystemTitle = computeSystem.DisplayName;
            cardViewModel.ComputeSystemWrapper.StateChanged += cardViewModel.OnComputeSystemStateChanged;
            cardViewModel.CardState = await cardViewModel.GetCardStateAsync();
            cardViewModel.ComputeSystemImage = await GetBitmapImage(computeSystem);
            cardViewModel.ComputeSystemProperties = new List<ICardProperty>();
            var properties = await computeSystem.GetComputeSystemPropertiesAsync(string.Empty);
            foreach (var property in properties)
            {
                cardViewModel.ComputeSystemProperties.Add(new CardProperty(property));
            }
        }
        catch (Exception ex)
        {
            Log.Logger.ReportError(Log.Component.ComputeSystemViewModelFactory, $"Failed to get initial properties for compute system {computeSystem}. Error: {ex.Message}");
        }

        return cardViewModel;
    }

    private async Task<BitmapImage> GetBitmapImage(ComputeSystem computeSystemWrapper)
    {
        try
        {
            var result = await computeSystemWrapper.GetComputeSystemThumbnailAsync(string.Empty);

            if (result.Result.Status == ProviderOperationStatus.Failure || result.ThumbnailInBytes.Length <= 0)
            {
                Log.Logger.ReportError(Log.Component.ComputeSystemViewModelFactory, $"Failed to get thumbnail for compute system {computeSystemWrapper}. Error: {result.Result.DiagnosticText}");

                // No thumbnail available, return null so that the card will display the default image.
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.SetSource(result.ThumbnailInBytes.AsBuffer().AsStream().AsRandomAccessStream());
            return bitmap;
        }
        catch (Exception ex)
        {
            Log.Logger.ReportError(Log.Component.ComputeSystemViewModelFactory, $"Failed to get thumbnail for compute system {computeSystemWrapper}. Error: {ex.Message}");
            return null;
        }
    }
}
