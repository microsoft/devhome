// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
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
    private readonly ObservableCollection<ExternalTool> externalTools = [];
    private readonly string toolInfoFileName;

    public static readonly ExternalToolsHelper Instance = new();

    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalToolsHelper));

    public ReadOnlyObservableCollection<ExternalTool> ExternalTools { get; set; }

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

        toolInfoFileName = Path.Combine(localFolder, "externaltools.json");
        ExternalTools = new(externalTools);
    }

    internal void Init()
    {
        if (File.Exists(toolInfoFileName))
        {
            try
            {
                var jsonData = File.ReadAllText(toolInfoFileName);
                var existingData = JsonSerializer.Deserialize<ExternalTool[]>(jsonData) ?? [];
                foreach (var data in existingData)
                {
                    externalTools.Add(data);
                }
            }
            catch (Exception ex)
            {
                // TODO If we failed parsing the JSON file... should we just delete it?
                _log.Error(ex, "Failed to parse {tool}", toolInfoFileName);
            }
        }
    }

    public ExternalTool AddExternalTool(ExternalTool tool)
    {
        externalTools.Add(tool);

        // Write out to JSON file
        var updatedJson = JsonSerializer.Serialize(externalTools, serializerOptions);

        try
        {
            File.WriteAllText(toolInfoFileName, updatedJson);
        }
        catch (Exception ex)
        {
            // TODO What should we do if we're unable to write to the file?
            _log.Error(ex, "AddExternalTool unable to write to file");
        }

        return tool;
    }

    public void RemoveExternalTool(ExternalTool tool)
    {
        if (externalTools.Remove(tool))
        {
            // Write out to JSON file
            var updatedJson = JsonSerializer.Serialize(externalTools, serializerOptions);

            try
            {
                File.WriteAllText(toolInfoFileName, updatedJson);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "RemoveExternalTool unable to write to file");
            }
        }
    }
}
