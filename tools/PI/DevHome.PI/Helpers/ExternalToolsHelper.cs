// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    private readonly ObservableCollection<ExternalTool> filteredExternalTools = [];

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
    public ReadOnlyObservableCollection<ExternalTool> FilteredExternalTools { get; private set; }

    internal static int ToolsCollectionVersion { get; private set; } = 2;

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
        // %LocalAppData%\Packages\Microsoft.Windows.DevHome_8wekyb3d8bbwe\LocalState\externaltools.json
        toolInfoFileName = Path.Combine(localFolder, "externaltools.json");
        AllExternalTools = new(allExternalTools);
        FilteredExternalTools = new(filteredExternalTools);
    }

    internal void Init()
    {
        allExternalTools.Clear();
        if (File.Exists(toolInfoFileName))
        {
            var jsonData = File.ReadAllText(toolInfoFileName);
            try
            {
                var toolCollection = JsonSerializer.Deserialize<ExternalToolCollection>(jsonData);
                var existingData = toolCollection?.ExternalTools ?? [];
                foreach (var toolItem in existingData)
                {
                    allExternalTools.Add(toolItem);
                    toolItem.PropertyChanged += ToolItem_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to parse {toolInfoFileName}, attempting migration");
                MigrateOldTools(jsonData);
            }
        }
    }

    private void MigrateOldTools(string jsonData)
    {
        try
        {
            var oldFormatData = JsonSerializer.Deserialize<ExternalTool_v1[]>(jsonData) ?? [];
            foreach (var oldTool in oldFormatData)
            {
                var arguments = string.Empty;
                if (oldTool.ArgType == ExternalToolArgType.ProcessId)
                {
                    arguments = $" {oldTool.ArgPrefix}{{pid}} {oldTool.OtherArgs}";
                }
                else if (oldTool.ArgType == ExternalToolArgType.Hwnd)
                {
                    arguments = $" {oldTool.ArgPrefix}{{hwnd}} {oldTool.OtherArgs}";
                }
                else
                {
                    arguments = oldTool.OtherArgs;
                }

                var newTool = new ExternalTool(
                    oldTool.Name,
                    oldTool.Executable,
                    ToolActivationType.Launch,
                    arguments,
                    string.Empty,
                    string.Empty,
                    oldTool.IsPinned);

                allExternalTools.Add(newTool);
                newTool.PropertyChanged += ToolItem_PropertyChanged;
            }

            // Write out the updated data with the new file format.
            WriteToolsJsonFile();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to migrate old tools");
        }
    }

    private void ToolItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The user can change the IsPinned property of a tool, to pin or unpin it on the bar.
        if (sender is ExternalTool tool && string.Equals(e.PropertyName, nameof(ExternalTool.IsPinned), StringComparison.Ordinal))
        {
            if (tool.IsPinned)
            {
                if (!filteredExternalTools.Contains(tool))
                {
                    filteredExternalTools.Add(tool);
                }
            }
            else
            {
                filteredExternalTools.Remove(tool);
            }
        }

        // Only update the JSON file if the property is not attributed [JsonIgnore].
        if (!IsJsonIgnoreProperty<ExternalTool>(e.PropertyName))
        {
            WriteToolsJsonFile();
        }
    }

    private bool IsJsonIgnoreProperty<T>(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        var property = typeof(T).GetProperty(propertyName);
        if (property is not null)
        {
            var jsonIgnoreAttribute = property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).FirstOrDefault();
            return jsonIgnoreAttribute is not null;
        }

        return false;
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
        var toolCollection = new ExternalToolCollection(ToolsCollectionVersion, allExternalTools);
        var updatedJson = JsonSerializer.Serialize(toolCollection, serializerOptions);

        try
        {
            File.WriteAllText(toolInfoFileName, updatedJson);
        }
        catch (Exception ex)
        {
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
                            filteredExternalTools.Add(newItem);
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
                        filteredExternalTools.Remove(oldItem);
                    }
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is not null)
                {
                    foreach (ExternalTool oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= ToolItem_PropertyChanged;
                        filteredExternalTools.Remove(oldItem);
                    }
                }

                if (e.NewItems is not null)
                {
                    foreach (ExternalTool newItem in e.NewItems)
                    {
                        if (newItem.IsPinned)
                        {
                            filteredExternalTools.Add(newItem);
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
        filteredExternalTools.Clear();
        foreach (var item in AllExternalTools)
        {
            if (item.IsPinned)
            {
                filteredExternalTools.Add(item);
            }
        }
    }
}
