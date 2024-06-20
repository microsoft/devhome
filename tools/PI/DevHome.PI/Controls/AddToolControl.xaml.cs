// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;

namespace DevHome.PI.Controls;

public sealed partial class AddToolControl : UserControl, INotifyPropertyChanged
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AddToolControl));

    private readonly string invalidToolInfo = CommonHelper.GetLocalizedString("InvalidToolInfoMessage");
    private readonly string messageCloseText = CommonHelper.GetLocalizedString("MessageCloseText");

    // We have 3 sets of operations, and we arbitrarily divide the progress timing into 3 equal segments.
    private const int ShortcutProcessingEndIndex = 33;
    private const int PackageProcessingEndIndex = 67;

    private InstalledAppInfo? selectedApp;
    private List<string>? shortcuts;
    private List<Package>? packages;
    private List<InstalledAppInfo> allApps = [];
    private int itemCount;

    public ObservableCollection<InstalledAppInfo> SortedApps { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isLoading;

    public bool IsLoading
    {
        get => isLoading;
        set
        {
            isLoading = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
        }
    }

    private int progressPercentage;

    public int ProgressPercentage
    {
        get => progressPercentage;
        set
        {
            progressPercentage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressPercentage)));
        }
    }

    public AddToolControl()
    {
        InitializeComponent();
        LoadingProgressTextRing.DataContext = this;
    }

    private void ToolBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        HandleBrowseButton();
    }

    private void HandleBrowseButton()
    {
        // WinUI3's OpenFileDialog does not work if we're running elevated. So we have to use the old Win32 API.
        var fileName = string.Empty;
        var filter = "Executables (*.exe)\0*.exe\0Batch Files (*.bat)\0*.bat\0\0";
        var filterarray = filter.ToCharArray();
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;

        unsafe
        {
            fixed (char* file = new char[255], pFilter = filterarray)
            {
                var openfile = new OPENFILENAMEW
                {
                    lStructSize = (uint)Marshal.SizeOf<OPENFILENAMEW>(),
                    lpstrFile = new Windows.Win32.Foundation.PWSTR(file),
                    lpstrFilter = pFilter,
                    nFilterIndex = 1,
                    nMaxFile = 255,

                    // TODO - This should be the Settings window, not the bar window
                    hwndOwner = barWindow?.CurrentHwnd ?? Windows.Win32.Foundation.HWND.Null,
                };

                if (PInvoke.GetOpenFileName(ref openfile))
                {
                    fileName = new string(openfile.lpstrFile);
                }
            }
        }

        if (!string.IsNullOrEmpty(fileName))
        {
            ToolPathTextBox.Text = fileName;
            ToolNameTextBox.Text = Path.GetFileNameWithoutExtension(fileName);
        }

        return;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var tool = GetCurrentToolDefinition();
        if (tool is null)
        {
            return;
        }

        ExternalToolsHelper.Instance.AddExternalTool(tool);
        var toolRegisteredMessage = CommonHelper.GetLocalizedString("ToolRegisteredMessage", ToolNameTextBox.Text);
        WindowHelper.ShowTimedMessageDialog(this, toolRegisteredMessage, messageCloseText);
        ClearValues();
    }

    private void ClearValues()
    {
        ToolNameTextBox.Text = string.Empty;
        ToolPathTextBox.Text = string.Empty;
        LaunchRadio.IsChecked = true;
        ArgumentsTextBox.Text = string.Empty;
        IsPinnedToggleSwitch.IsOn = true;
        selectedApp = null;
    }

    private ExternalTool? GetCurrentToolDefinition()
    {
        if (string.IsNullOrEmpty(ToolNameTextBox.Text) || string.IsNullOrEmpty(ToolPathTextBox.Text))
        {
            WindowHelper.ShowTimedMessageDialog(this, invalidToolInfo, messageCloseText);
            return null;
        }

        var activationType = ToolActivationType.Launch;
        if (ProtocolRadio.IsChecked ?? false)
        {
            activationType = ToolActivationType.Protocol;
        }
        else if (selectedApp is not null && selectedApp.IsMsix)
        {
            activationType = ToolActivationType.Msix;
        }

        return new(
            ToolNameTextBox.Text,
            ToolPathTextBox.Text,
            activationType,
            ArgumentsTextBox.Text,
            selectedApp?.AppUserModelId ?? string.Empty,
            selectedApp?.IconFilePath ?? string.Empty,
            IsPinnedToggleSwitch.IsOn);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ClearValues();
    }

    private async void AppListButton_Click(object sender, RoutedEventArgs e)
    {
        allApps.Clear();
        SortedApps.Clear();

        itemCount = GetShortcuts();
        if (itemCount == 0)
        {
            _log.Error("Error getting shortcuts");
        }

        var packageCount = GetPackages();
        if (packageCount == 0)
        {
            _log.Error("Error getting packages");
        }

        itemCount += packageCount;
        if (itemCount == 0)
        {
            _log.Error("Error getting list of installed apps");
            return;
        }

        // We get most of the data on a background thread, which
        // reports intermittent progress.
        IsLoading = true;
        var progress = new Progress<int>(percent =>
        {
            // Update the progress report.
            LoadingProgressTextRing.Value = percent;
        });

        await ProcessItemsAsync(progress);

        foreach (var app in allApps)
        {
            SortedApps.Add(app);
        }

        IsLoading = false;
    }

    private int GetShortcuts()
    {
        int count;

        // Search for .lnk files in the per-user and all-users Start Menu Programs directories.
        // %APPDATA%\Microsoft\Windows\Start Menu\Programs
        var startMenuProgramsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

        // %ProgramData%\Microsoft\Windows\Start Menu\Programs
        var commonStartMenuProgramsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");

        shortcuts = [];
        if (Directory.Exists(startMenuProgramsPath))
        {
            shortcuts.AddRange(Directory.GetFiles(startMenuProgramsPath, "*.lnk", SearchOption.AllDirectories));
        }

        if (Directory.Exists(commonStartMenuProgramsPath))
        {
            shortcuts.AddRange(Directory.GetFiles(commonStartMenuProgramsPath, "*.lnk", SearchOption.AllDirectories));
        }

        count = shortcuts?.Count ?? 0;
        return count;
    }

    private int GetPackages()
    {
        var count = 0;
        var packageManager = new PackageManager();
        packages = packageManager.FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main).ToList();
        if (packages is not null)
        {
            count = packages.Count;
        }

        return count;
    }

    private async Task ProcessItemsAsync(IProgress<int> progress)
    {
        // Process all the shortcut files.
        var currentCount = 0;
        for (var i = 0; i < shortcuts?.Count; i++)
        {
            await Task.Run(() => ProcessShortcut(shortcuts[i]));

            // Report progress.
            currentCount++;
            var percentComplete = (i + 1) * ShortcutProcessingEndIndex / itemCount;
            progress.Report(percentComplete);
        }

        for (var j = 0; j < packages?.Count; j++)
        {
            await Task.Run(() => ProcessPackage(packages[j]));

            // Report progress.
            currentCount++;
            var percentComplete = ShortcutProcessingEndIndex + ((j + 1) * ShortcutProcessingEndIndex / itemCount);
            progress.Report(percentComplete);
        }

        allApps = allApps.OrderBy(app => app.Name).ToList();

        // We get the icon data on the UI thread, because BitmapImages must be created on the UI thread.
        for (var k = 0; k < allApps.Count; k++)
        {
            var app = allApps[k];
            if (app.IsMsix)
            {
                if (app.AppPackage is not null)
                {
                    try
                    {
                        // The package might be in a bad state, and accessing its
                        // properties might throw an exception.
                        app.Icon = new BitmapImage(app.AppPackage.Logo);
                        app.IconFilePath = app.AppPackage.Logo.LocalPath;
                    }
                    catch
                    {
                        _log.Error("Error getting icon from package");
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(app.TargetPath))
                {
                    app.Icon = WindowHelper.GetBitmapImageFromFile(app.TargetPath);
                }
            }

            currentCount++;
            var percentComplete = PackageProcessingEndIndex + ((k + 1) * ShortcutProcessingEndIndex / itemCount);
            progress.Report(percentComplete);

            // Yield to make sure the UI thread can update the progress output.
            await Task.Delay(1);
        }
    }

    public void ProcessShortcut(string filePath)
    {
        var appName = Path.GetFileNameWithoutExtension(filePath);

        // Exclude Microsoft Virtual Desktop and WSA shortcuts.
        if (appName.Contains("Microsoft Virtual Desktop", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var wshShell = new WshShell();
        if (wshShell.CreateShortcut(filePath) is not IWshShortcut shortcut)
        {
            _log.Error("Error getting shortcut");
            return;
        }

        // Proceed with using the shortcut object.
        var targetPath = shortcut.TargetPath;

        // Exclude shortcuts that point to empty targets, filesystem folders, or WSA apps.
        if (string.IsNullOrEmpty(targetPath)
            || Directory.Exists(targetPath)
            || targetPath.Contains("WsaClient.exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Exclude *.chm, *.url, *.html, *.ico targets.
        var extension = Path.GetExtension(targetPath);
        if (extension.Equals(".chm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".url", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Add the app for this shortcut to the list.
        allApps.Add(new InstalledAppInfo
        {
            Name = appName,
            ShortcutFilePath = filePath,
            TargetPath = targetPath,
        });
    }

    public void ProcessPackage(Package package)
    {
        var op = package.GetAppListEntriesAsync();
        var task = op.AsTask();
        task.Wait();
        var entries = task.Result;

        // We only get the icon for apps that have an AppListEntry,
        // because the others are not likely to be activatable from the UI.
        if (entries.Count > 0)
        {
            // We only get the icon for the first AppListEntry, ignoring MAPs.
            // Note we use Package.Logo, not AppListEntry.DisplayInfo.GetLogo
            // because the latter doesn't get consistently-sized icons.
            var appListEntry = entries[0];

            // Add the app for this package to the list.
            allApps.Add(new InstalledAppInfo
            {
                Name = package.DisplayName,
                AppUserModelId = appListEntry.AppUserModelId,
                AppPackage = package,
            });
        }
    }

    private void AppsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppsListView.SelectedItem is InstalledAppInfo app)
        {
            selectedApp = app;
            if (!string.IsNullOrEmpty(app.TargetPath))
            {
                ToolPathTextBox.Text = app.TargetPath;
            }
            else if (!string.IsNullOrEmpty(app.AppUserModelId))
            {
                ToolPathTextBox.Text = app.AppUserModelId;
            }

            ToolNameTextBox.Text = app.Name;
        }
        else
        {
            selectedApp = null;
            ToolPathTextBox.Text = string.Empty;
            ToolNameTextBox.Text = string.Empty;
        }
    }
}
