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
using Newtonsoft.Json;

namespace DevHome.Projects.ViewModels;
public partial class ProjectViewModel : ObservableObject
{
    public string Name { get; set; }

    public string Url { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandedFilePath))]
    private string filePath;

    [JsonIgnore]
    public string ExpandedFilePath => Environment.ExpandEnvironmentVariables(FilePath);

    public string Color { get; set; } = "Transparent";

    public ObservableCollection<LauncherViewModel> Launchers { get; } = new ();
}
