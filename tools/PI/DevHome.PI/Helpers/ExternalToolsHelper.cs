// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DevHome.Common.Helpers;
using Serilog;
using Windows.Storage;

namespace DevHome.PI.Helpers;

internal sealed class ExternalToolsHelper
{
    private readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
    private readonly string toolInfoFileName;

    public static readonly ExternalToolsHelper Instance = new();

    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalToolsHelper));

    private ObservableCollection<ExternalTool> allExternalTools = [];

    // The ExternalTools menu shows all registered tools.
    public ObservableCollection<ExternalTool> AllExternalTools
    {
        get => allExternalTools;
        set
        {
            // We're assigning the collection once, and this also covers the case where we reassign it again:
            // we need to unsubscribe from the old collection's events, subscribe to the new collection's events,
            // and initialize the filtered collection.
            if (allExternalTools != value)
            {
                if (allExternalTools != null)
                {
                    allExternalTools.CollectionChanged -= AllExternalTools_CollectionChanged;
                }

                allExternalTools = value;
                if (allExternalTools != null)
                {
                    allExternalTools.CollectionChanged += AllExternalTools_CollectionChanged;
                }

                // Synchronize the filtered collection with this unfiltered one.
                SynchronizeAllFilteredItems();
            }
        }
    }

    // The bar shows only the pinned tools.
    public ObservableCollection<ExternalTool> FilteredExternalTools { get; private set; } = [];

    private ExternalToolsHelper()
    {
        string localFolder;
        if (RuntimeHelper.IsMSIX)
        {
            localFolder = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            localFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty;
        }

        // The file should be in this location:
        // %LocalAppData%\Packages\Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe\LocalState\externaltools.json
        toolInfoFileName = Path.Combine(localFolder, "externaltools.json");
        AllExternalTools = new(allExternalTools);
    }

    internal void Init()
    {
        allExternalTools.Clear();
        if (File.Exists(toolInfoFileName))
        {
            try
            {
                var jsonData = File.ReadAllText(toolInfoFileName);
                var existingData = JsonSerializer.Deserialize<ExternalTool[]>(jsonData) ?? [];
                foreach (var toolItem in existingData)
                {
                    allExternalTools.Add(toolItem);
                    toolItem.PropertyChanged += ToolItem_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                // TODO If we failed to parse the JSON file, we should rename it (using DateTime.Now),
                // create a new one, and report to the user.
                _log.Error(ex, "Failed to parse {tool}", toolInfoFileName);
            }
        }
    }

    private void ToolItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The user can change the IsPinned property of a tool, to pin or unpin it on the bar.
        if (sender is ExternalTool tool && e.PropertyName == nameof(ExternalTool.IsPinned))
        {
            if (tool.IsPinned)
            {
                if (!FilteredExternalTools.Contains(tool))
                {
                    FilteredExternalTools.Add(tool);
                }
            }
            else
            {
                FilteredExternalTools.Remove(tool);
            }
        }

        WriteToolsJsonFile();
    }

    public ExternalTool AddExternalTool(ExternalTool tool)
    {
        allExternalTools.Add(tool);
        WriteToolsJsonFile();
        return tool;
    }

    public void RemoveExternalTool(ExternalTool tool)
    {
        if (allExternalTools.Remove(tool))
        {
            WriteToolsJsonFile();
        }
    }

    private void WriteToolsJsonFile()
    {
        var updatedJson = JsonSerializer.Serialize(allExternalTools, serializerOptions);

        try
        {
            File.WriteAllText(toolInfoFileName, updatedJson);
        }
        catch (Exception ex)
        {
            // TODO If we're unable to write to the file, we should figure out why.
            // If the file has become corrupted, we should rename it (using DateTime.Now),
            // create a new one, and report to the user. If it's locked, we just report to the user.
            _log.Error(ex, "WriteToolsJsonFile unable to write to file");
        }
    }

    private void AllExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Whenever the "all tools" collection changes, we need to synchronize the filtered collection.
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (ExternalTool newItem in e.NewItems)
                    {
                        if (newItem.IsPinned)
                        {
                            FilteredExternalTools.Add(newItem);
                        }

                        newItem.PropertyChanged += ToolItem_PropertyChanged;
                    }
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (ExternalTool oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= ToolItem_PropertyChanged;
                        FilteredExternalTools.Remove(oldItem);
                    }
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is not null)
                {
                    foreach (ExternalTool oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= ToolItem_PropertyChanged;
                        FilteredExternalTools.Remove(oldItem);
                    }
                }

                if (e.NewItems is not null)
                {
                    foreach (ExternalTool newItem in e.NewItems)
                    {
                        if (newItem.IsPinned)
                        {
                            FilteredExternalTools.Add(newItem);
                        }

                        newItem.PropertyChanged += ToolItem_PropertyChanged;
                    }
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                SynchronizeAllFilteredItems();
                break;
        }
    }

    private void SynchronizeAllFilteredItems()
    {
        FilteredExternalTools.Clear();
        foreach (var item in AllExternalTools)
        {
            if (item.IsPinned)
            {
                FilteredExternalTools.Add(item);
            }
        }
    }
}
