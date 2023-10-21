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
            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.Combine(Path.GetDirectoryName(location)!, @"Assets\projects.schema.json");
            return path;
        }
    }
}
