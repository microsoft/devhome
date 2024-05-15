// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Environments.ViewModels;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Models;

public class PinOperationData
{
    public OperationsViewModel? ViewModel { get; }

    public bool WasPinnedStatusSuccessful { get; }

    public string? PinnedStatusDisplayMessage { get; }

    public string? PinnedStatusDiagnosticText { get; }

    public PinOperationData(OperationsViewModel? viewModel, ComputeSystemPinnedResult pinnedResult)
    {
        ViewModel = viewModel;
        WasPinnedStatusSuccessful = pinnedResult.Result.Status == ProviderOperationStatus.Success;
        PinnedStatusDisplayMessage = pinnedResult.Result.DisplayMessage;
        PinnedStatusDiagnosticText = pinnedResult.Result.DiagnosticText;
    }
}
