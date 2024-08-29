// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevHome.Common.Renderers;

public class ChooseFileAction : IAdaptiveActionElement
{
    // ChooseFile properties
    public string FilePath { get; set; } = string.Empty;

    public string Verb { get; set; } = string.Empty;

    public string FileTypeFilter { get; set; } = "*";

    public bool UseIcon { get; set; }

    public static readonly string CustomTypeString = "Action.ChooseFile";

    // Inherited properties
    public ActionType ActionType => ActionType.Custom;

    public string ActionTypeString => CustomTypeString;

    public JsonObject? AdditionalProperties { get; set; }

    public IAdaptiveActionElement? FallbackContent { get; set; }

    public FallbackType FallbackType { get; set; } = FallbackType.Drop;

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
            ["fileTypeFilter"] = JsonValue.CreateStringValue(FileTypeFilter),
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
        var fileTypes = ParseFileTypeFilter(FileTypeFilter);

        var filePicker = new FileOpenPicker();
        foreach (var fileType in fileTypes)
        {
            filePicker.FileTypeFilter.Add(fileType);
        }

        var mainWindow = Application.Current.GetService<Window>();
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

    private List<string> ParseFileTypeFilter(string rawFileTypeFilter)
    {
        // If the length is 1, either it's a * or it's an invalid string, so we should default to "*" anyway.
        if (rawFileTypeFilter.Length == 0 || rawFileTypeFilter.Length == 1)
        {
            return ["*"];
        }

        List<string> fileTypes = [];

        // Create a regex for valid file extensions.
        var pattern = @"^\.[a-zA-Z0-9]+$";
        var regex = new Regex(pattern);

        var tokens = rawFileTypeFilter.Split(',');

        foreach (var token in tokens)
        {
            if (regex.IsMatch(token))
            {
                fileTypes.Add(token);
            }
            else
            {
                // If anything is not a valid file extension, throw away everything and use the extension wildcard.
                return ["*"];
            }
        }

        return fileTypes;
    }
}

public class ChooseFileParser : IAdaptiveActionParser
{
    public IAdaptiveActionElement FromJson(
        JsonObject inputJson,
        AdaptiveElementParserRegistration elementParsers,
        AdaptiveActionParserRegistration actionParsers,
        IList<AdaptiveWarning> warnings)
    {
        var stringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");

        var chooseFileAction = new ChooseFileAction
        {
            Title = stringResource.GetLocalized("ChooseFileActionTitle"),
            Tooltip = stringResource.GetLocalized("ChooseFileActionToolTip"),

            // Parse the JSON properties of the action.
            // The Verb ChooseFile is not meant to be localized.
            Verb = inputJson.GetNamedString("verb", "ChooseFile"),
            UseIcon = inputJson.GetNamedBoolean("useIcon", false),
            FileTypeFilter = inputJson.GetNamedString("fileTypeFilter", "*"),
        };

        return chooseFileAction;
    }
}

public class ChooseFileActionRenderer : IAdaptiveActionRenderer
{
    public UIElement Render(IAdaptiveActionElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AdaptiveExecuteActionRenderer();

        if (element as ChooseFileAction is ChooseFileAction chooseFileElement)
        {
            // Card author is not allowed to specify a custom icon for the file picker action.
            chooseFileElement.IconUrl = string.Empty;

            var button = renderer.Render(element, context, renderArgs) as Button;
            if (button != null)
            {
                var content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                };
                if (chooseFileElement.UseIcon)
                {
                    content.Children.Add(new FontIcon
                    {
                        Glyph = "\xED25",
                    });
                }

                if (!string.IsNullOrEmpty(chooseFileElement.Title))
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = chooseFileElement.Title,
                    });
                }

                button.Content = content;

                return button;
            }
        }

        return renderer.Render(element, context, renderArgs);
    }
}
