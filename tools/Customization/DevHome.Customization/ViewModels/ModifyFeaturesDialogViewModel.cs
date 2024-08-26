// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.Customization.ViewModels;

public partial class ModifyFeaturesDialogViewModel : ObservableObject
{
    private CancellationTokenSource? _cancellationTokenSource;

    public void SetCommittingChanges(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
    }

    internal void HandleCancel()
    {
        _cancellationTokenSource?.Cancel();
    }
}
