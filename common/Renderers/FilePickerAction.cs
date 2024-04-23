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
    public FilePickerAction()
    {
    }

    public ActionType ActionType => ActionType.Custom;

    public string ActionTypeString => CustomTypeString;

    public AdaptiveCards.ObjectModel.WinUI3.ActionMode Mode { get; set; }

    public ActionRole Role { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string Verb { get; set; } = string.Empty;

    public static readonly string CustomTypeString = "Action.ChooseFile";

    public JsonObject ToJson()
    {
        var json = new JsonObject
        {
            ["type"] = JsonValue.CreateStringValue(ActionTypeString),
            ["title"] = JsonValue.CreateStringValue(Title),
            ["filePath"] = JsonValue.CreateStringValue(FilePath),
            ["isVisible"] = JsonValue.CreateBooleanValue(IsVisible),
            ["isEnabled"] = JsonValue.CreateBooleanValue(IsEnabled),
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

    public JsonObject? AdditionalProperties { get; set; }

    public ElementType ElementType => ElementType.Custom;

    public string ElementTypeString => CustomTypeString;

    public IAdaptiveActionElement? FallbackContent { get; set; }

    public string IconUrl { get; set; } = string.Empty;

    public string Style { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Tooltip { get; set; } = string.Empty;

    public FallbackType FallbackType { get; set; }

    public HeightType Height { get; set; } = HeightType.Auto;

    public string? Id { get; set; } = CustomTypeString + "Id";

    public bool IsVisible { get; set; } = true;

    public bool IsEnabled { get; set; } = true;

    public bool Separator { get; set; }

    public Spacing Spacing { get; set; } = Spacing.Small;

    public void LaunchFilePicker()
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
        }
    }
}

public class FilePickerParser : IAdaptiveActionParser
{
    public IAdaptiveActionElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var filePickerAction = new FilePickerAction();

        // Parse the JSON properties of the action
        filePickerAction.Verb = inputJson.GetNamedString("verb", string.Empty);
        filePickerAction.Title = inputJson.GetNamedString("title", "%FilePickerTitle%");
        filePickerAction.Tooltip = inputJson.GetNamedString("tooltip", "%FilePickerToolTip%");

        return filePickerAction;
    }
}

/*
public class FilePickerRenderer : IAdaptiveElementRenderer
{
    public UIElement? Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        if (element is FilePickerAction filePicker)
        {
            var button = new Button
            {
                Content = filePicker.ButtonText,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 0),
            };

            button.Click += async (sender, args) =>
            {
                var pickFile = new FileOpenPicker();
                pickFile.FileTypeFilter.Add("*");

                var mainWindow = Application.Current.GetService<WindowEx>();
                if (mainWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(mainWindow);
                    InitializeWithWindow.Initialize(filePicker, hwnd);
                }

                var file = await pickFile.PickSingleFileAsync();
                if (file != null)
                {
                    filePicker.FilePath = file.Path;
                }
            };

            return button;
        }

        return null;
    }
}
*/

public class FilePickerExecuteAction : IAdaptiveActionRenderer
{
    public UIElement? Render(IAdaptiveActionElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AdaptiveExecuteActionRenderer();

        if (element is FilePickerAction)
        {
            var button = renderer.Render(element, context, renderArgs) as Button;

            if (button == null)
            {
                return null;
            }

            /*
            button.Content = "Choose File";
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.VerticalAlignment = VerticalAlignment.Center;
            button.Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 0);
            */

            /*
            button.Click += async (sender, args) =>
            {
                var filePicker = new FileOpenPicker();
                filePicker.FileTypeFilter.Add("*");

                var mainWindow = Application.Current.GetService<WindowEx>();
                if (mainWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(mainWindow);
                    InitializeWithWindow.Initialize(filePicker, hwnd);
                }

                var file = await filePicker.PickSingleFileAsync();
                if (file != null)
                {
                    filePickerAction.FilePath = file.Path;
                }
            };
            return button;
            */
            return button;
        }

        return renderer.Render(element, context, renderArgs);
    }
}
