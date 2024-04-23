// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUIEx;

namespace DevHome.Common.Renderers;

public class FilePickerAction : IAdaptiveActionElement
{
    // ChooseFile properties
    public string FilePath { get; set; } = string.Empty;

    public string Verb { get; set; } = string.Empty;

    public static readonly string CustomTypeString = "Action.ChooseFile";

    // Inherited properties
    public ActionType ActionType => ActionType.Custom;

    public string ActionTypeString => CustomTypeString;

    public JsonObject? AdditionalProperties { get; set; }

    public IAdaptiveActionElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; }

    public string IconUrl { get; set; } = string.Empty;

    public string? Id { get; set; } = CustomTypeString + "Id";

    public bool IsEnabled { get; set; } = true;

    public AdaptiveCards.ObjectModel.WinUI3.ActionMode Mode { get; set; }

    public ActionRole Role { get; set; }

    public string Style { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Tooltip { get; set; } = string.Empty;

    public JsonObject ToJson()
    {
        var json = new JsonObject
        {
            ["type"] = JsonValue.CreateStringValue(ActionTypeString),
            ["filePath"] = JsonValue.CreateStringValue(FilePath),
            ["verb"] = JsonValue.CreateStringValue(Verb),
        };

        if (AdditionalProperties != null)
        {
            foreach (var prop in AdditionalProperties)
            {
                json.Add(prop.Key, prop.Value);
            }
        }

        return json;
    }

    /// <summary>Launches the file picker dialog to select a file.</summary>
    /// <returns>true if a file was selected, false otherwise.</returns>
    public bool LaunchFilePicker()
    {
        var filePicker = new FileOpenPicker();
        filePicker.FileTypeFilter.Add("*");

        var mainWindow = Application.Current.GetService<WindowEx>();
        if (mainWindow != null)
        {
            var hwnd = WindowNative.GetWindowHandle(mainWindow);
            InitializeWithWindow.Initialize(filePicker, hwnd);
        }

        var file = filePicker.PickSingleFileAsync().AsTask().Result;
        if (file != null)
        {
            FilePath = file.Path;
            return true;
        }

        return false;
    }
}

public class FilePickerParser : IAdaptiveActionParser
{
    public IAdaptiveActionElement FromJson(
        JsonObject inputJson,
        AdaptiveElementParserRegistration elementParsers,
        AdaptiveActionParserRegistration actionParsers,
        IList<AdaptiveWarning> warnings)
    {
        var filePickerAction = new FilePickerAction();

        // Parse the JSON properties of the action
        filePickerAction.Verb = inputJson.GetNamedString("verb", string.Empty);
        filePickerAction.Title = inputJson.GetNamedString("title", "%FilePickerTitle%");
        filePickerAction.Tooltip = inputJson.GetNamedString("tooltip", "%FilePickerToolTip%");

        return filePickerAction;
    }
}

public class FilePickerExecuteAction : IAdaptiveActionRenderer
{
    public UIElement? Render(IAdaptiveActionElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AdaptiveExecuteActionRenderer();

        if (element is FilePickerAction)
        {
            var button = renderer.Render(element, context, renderArgs) as Button;

            return button;
        }

        return renderer.Render(element, context, renderArgs);
    }
}
