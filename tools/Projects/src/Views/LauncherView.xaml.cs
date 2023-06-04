// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using DevHome.Projects.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Windows.Win32.PInvoke;

namespace DevHome.Projects.Views;
public sealed partial class LauncherView : UserControl
{
    public LauncherView()
    {
        this.InitializeComponent();
    }

    private void LauncherButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var project = (sender as Button).DataContext as LauncherViewModel;
        project?.Launch();
    }

    private LauncherViewModel ViewModel => (LauncherViewModel)this.DataContext;

    private string ResolvePath(string inputPath, string launcherDirectory = null)
    {
        var cwd = ViewModel.ProjectViewModel.TryGetTarget(out var project) ? project.ExpandedFilePath : string.Empty;
        Span<char> arr = stackalloc char[1024];
        inputPath.CopyTo(arr);

        unsafe
        {
            char** extra = stackalloc char*[3];
            fixed (char* cwdPtr = cwd, launcherDirectoryPtr = launcherDirectory)
            {
                extra[0] = cwdPtr;
                extra[1] = launcherDirectoryPtr;
                extra[2] = null;
                var res = PathFindOnPath(ref arr, (ushort**)extra);
                if (!res)
                {
                    return null;
                }
            }
        }

        var path = arr.ToString();
        return path;
    }

    public Image GetImage(object dataContext)
    {
        var launcher = dataContext as LauncherViewModel;
        var commandLine = launcher.CommandLine;
        var iconPath = Environment.ExpandEnvironmentVariables(launcher.IconPath ?? string.Empty);
        var argv = launcher.GetArgv();
        var launcherPath = ResolvePath(argv[0]);

        string path;
        if (!string.IsNullOrEmpty(iconPath))
        {
            if (!Path.IsPathFullyQualified(iconPath))
            {
                path = ResolvePath(iconPath, Path.GetDirectoryName(launcherPath));
            }
            else
            {
                path = iconPath;
            }
        }
        else
        {
            path = launcherPath;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        // load the icon bitmap from the program in argv[0]
        var file = Windows.Storage.StorageFile.GetFileFromPathAsync(path).AsTask().Result;
        var icon = file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem).AsTask().Result;
        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
        bitmap.SetSource(icon);
        return new Image { Source = bitmap, Width = 24, Height = 24 };
    }
}
