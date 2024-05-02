// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace DevHome.Utilities.ViewModels;

public class UtilityViewModel : INotifyPropertyChanged
{
    private readonly string exeName;

    public string Title { get; set; }

    public string Description { get; set; }

    public string NavigateUri { get; set; }

    public string ImageSource { get; set; }

    public ICommand LaunchCommand { get; set; }

    public ICommand LaunchAsAdminCommand { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public UtilityViewModel(string exeName)
    {
        this.exeName = exeName;
        LaunchCommand = new RelayCommand(Launch);
        LaunchAsAdminCommand = new RelayCommand(LaunchAsAdmin);
    }

    private void Launch()
    {
        LaunchInternal(false);
    }

    private void LaunchAsAdmin()
    {
        LaunchInternal(true);
    }

    private void LaunchInternal(bool runAsAdmin)
    {
        // We need to start the process with ShellExecute to run elevated
        var processStartInfo = new ProcessStartInfo
        {
            FileName = exeName,
            UseShellExecute = true,

            Verb = runAsAdmin ? "runas" : "open",
        };

        var process = Process.Start(processStartInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start background process");
        }
    }
}
