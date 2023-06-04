// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace DevHome.Projects.ViewModels;

public class ProjectsViewModel : ObservableRecipient
{
    public ObservableCollection<ProjectViewModel> Projects { get; } = new ();

    public ProjectsViewModel()
    {
    }
}
