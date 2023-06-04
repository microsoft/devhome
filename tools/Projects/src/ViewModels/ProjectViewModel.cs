// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Projects.ViewModels;
public class ProjectViewModel : ObservableObject
{
    public string Name { get; set; }

    public string Url { get; set; }

    public string FilePath { get; set; }

    public string Color { get; set; } = "Transparent";

    public ObservableCollection<LauncherViewModel> Launchers { get; } = new ();
}
