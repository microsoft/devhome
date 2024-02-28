// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.ViewModels;

public partial class CloneRepoSummaryInformationViewModel : ObservableRecipient, ISummaryInformationViewModel
{
    [ObservableProperty]
    private string _fileName;

    [ObservableProperty]
    private string _repoName;
}
