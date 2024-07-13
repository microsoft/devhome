// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Projects.Views;
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

    internal static string JsonFilePath => Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\DevHome.projects.json");

    internal void SerializeViewModel()
    {
        var jsonStr = JsonConvert.SerializeObject(this, Formatting.Indented, new ObservableRecipientConverter());
        File.WriteAllText(JsonFilePath, jsonStr);
    }

    private async Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern, SearchOption so = SearchOption.TopDirectoryOnly)
    {
        return await Task.Run(() =>
        {
            return Directory.EnumerateFiles(path, searchPattern, so);
        });
    }

    public static ProjectsViewModel CreateViewModel()
    {
        Thread.Sleep(300); // wait for Defender to release the lock
        for (int i = 0; i < 5; i++)
        {
            if (File.Exists(JsonFilePath))
            {
                var jsonStr = File.ReadAllText(JsonFilePath);
                var vm = JsonConvert.DeserializeObject<ProjectsViewModel>(jsonStr);
                if (vm == null)
                {
                    Thread.Sleep(300); // wait for Defender to release the lock
                    continue;
                }

                foreach (var p in vm.Projects)
                {
                    foreach (var l in p.Launchers)
                    {
                        l.ProjectViewModel = new WeakReference<ProjectViewModel>(p);
                    }
                }

                return vm;
            }
        }

        return new ProjectsViewModel();
    }

    public async Task<ProjectViewModel> AddProject(string repositoryName, string filePath, Uri repoUri, string color = null)
    {
        var p = Projects.Where(p => p.FilePath == filePath);
        if (p.Any())
        {
            return p.First();
        }

        if (color == null)
        {
            var randomRGB = Random.Shared.Next(0, 1 << 24);
            color = $"#{randomRGB:X6}";
        }

        var project = new ProjectViewModel
        {
            Name = repositoryName,
            FilePath = filePath,
            Url = repoUri.ToString(),
            Color = color,
        };

        Projects.Add(project);

        // do we have an sln file?
        var slnFile = (await EnumerateFilesAsync(filePath, "*.sln")).FirstOrDefault();
        if (slnFile != null)
        {
            project.Launchers.Add(new LauncherViewModel
            {
                CommandLine = $"\"{slnFile}\"",
                DisplayName = Path.GetFileNameWithoutExtension(slnFile),
                ProjectViewModel = new WeakReference<ProjectViewModel>(project),
            });
        }

        return project;
    }
}
