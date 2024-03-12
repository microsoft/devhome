// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Hosting;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class CloneRepoSummaryInformationViewModel : ObservableRecipient, ISummaryInformationViewModel
{
    public string FileName => Path.GetFileName(FilePathAndName);

    public bool HasContent => !string.IsNullOrEmpty(FilePathAndName) && !string.IsNullOrEmpty(RepoName);

    [ObservableProperty]
    private string _filePathAndName;

    [ObservableProperty]
    private string _repoName;

    [RelayCommand]
    public void OpenFileInExplorer()
    {
        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;
        processStartInfo.FileName = Path.GetDirectoryName(FilePathAndName);

        Process.Start(processStartInfo);
    }
}
