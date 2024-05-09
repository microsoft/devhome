// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.SetupFlow.Views.Environments;

public sealed partial class SelectEnvironmentProviderView : UserControl
{
    public SelectEnvironmentProviderViewModel ViewModel => (SelectEnvironmentProviderViewModel)this.DataContext;

    public SelectEnvironmentProviderView()
    {
        this.InitializeComponent();
    }

    // This view needs to find the first selectable element because this is not the same view
    // with the shimmer.  By default, the focus is on the hamburger icon.  Change it.
    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Initialize(NotificationQueue);

        var numberOfSleeps = 0;
        var maxNumberOfSleeps = 10;

        // ViewModel can become null if the user cancels before loading is finished.
        // Wait 10 seconds for the adaptive cards to load.
        while (ViewModel != null && (!ViewModel.AreProvidersLoaded && numberOfSleeps++ < maxNumberOfSleeps))
        {
            await Task.Delay(1000);
        }

        if (ViewModel == null)
        {
            return;
        }

        if (ViewModel.AreProvidersLoaded)
        {
            // Focus on the first focusable element inside the shell content
            var element = FocusManager.FindFirstFocusableElement(SelectEnvironmentsLandingPage);
            if (element != null)
            {
                await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
            }
        }
    }
}
