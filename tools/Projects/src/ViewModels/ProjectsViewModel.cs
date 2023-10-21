// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace DevHome.Projects.ViewModels;

public class ProjectsViewModel : ObservableRecipient
{
    public ObservableCollection<ProjectViewModel> Projects { get; } = new ();

    public ProjectsViewModel()
    {
    }

    [JsonProperty("$schema")]
    public string Schema
    {
        get
        {
            return "https://aka.ms/devhome/projects.schema.json";
        }
    }
}
