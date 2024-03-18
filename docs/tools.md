# Tools

Dev Home adds functionality through a set of tools. Each tool provides a page in the Dev Home navigation view. Currently, all tools come from in-package assemblies. Third party or out-of-process tools are not supported at this time.

Tools utilize data and functionality from out-of-process [extensions](./extensions.md). This is done through the Extension SDK API. 

## Writing a Tool

1. Create a new directory with your tool's name under `tools` with three subdirectories `src`, `test`, and `uitest`
1. Create a new `WinUI 3 Class Library` project in your `src` directory
1. Create the `Strings\en-us` directories under `src`. Add `Resources.resw` and include the following code:
    ```xml
    <data name="NavigationPane.Content" xml:space="preserve">
      <value>[Name of your tool that will appear in navigation menu]</value>
      <comment>[Extra information about the name of your tool that may help translation]</comment>
    </data>
    ```
1. Add a project reference from `DevHome` to your project
1. Add a project reference from your project to `DevHome.Common.csproj` project under [/common/](/common)
1. Create your XAML View and ViewModel. Your View class must inherit from `ToolPage` and implement [tool interface requirements](#tool-requirements).
1. Update [NavConfig.jsonc](/src/NavConfig.jsonc) with your tool. Specifications for the [NavConfig.json schema](navconfig.md).

### Tool requirements

Each tool must define a custom page view extending from the [`ToolPage`](../common/ToolPage.cs) abstract class, and implement it like in this example:

```cs
public class SampleToolPage : ToolPage
{
    public override string ShortName => "SampleTool";

    public SampleToolPage()
    {
        ViewModel = Application.Current.GetService<SampleToolViewModel>();
        InitializeComponent();
    }
}
```

The Dev Home framework will look at all types in its assembly for any inheriting from `ToolPage`:

On a found type, the framework will use:
  - ShortName property to get the name of the tool

### Method definition

This section contains a more detailed description of each of the interface methods.

ShortName

```cs
public abstract string ShortName { get; }
```

Returns the name of the tool. This is used for the navigation menu text.

### Code organization

[`toolpage.cs`](../common/ToolPage.cs)
Contains the interface definition for Dev Home tools.

## Dashboard Tool
The Dashboard page hosts and displays Windows Widgets. Widgets are small UI containers that display text and graphics, associated with an app installed on the device. For information on creating widgets, see [Widgets Overview](https://learn.microsoft.com/windows/apps/design/widgets/) and [Widget providers](https://learn.microsoft.com/windows/apps/develop/widgets/widget-providers).

Each widget is represented by a [`WidgetViewModel`](../tools/Dashboard/DevHome.Dashboard/ViewModels/WidgetViewModel.cs). The WidgetViewModel displays itself inside a [`WidgetControl`](../tools/Dashboard/DevHome.Dashboard/Controls/WidgetControl.xaml), and the WidgetControls are grouped into a [`WidgetBoard`](../tools/Dashboard/DevHome.Dashboard/Controls/WidgetBoard.cs).

### Widget UI

The Widget UI consists of two main parts. At the top, there is a context menu and an attribution area. For more information on these components, please read [Built-in widget UI components](https://learn.microsoft.com/windows/apps/design/widgets/widgets-states-and-ui#built-in-widget-ui-components). The rest of the widget content is an [Adaptive Card](https://learn.microsoft.com/windows/apps/design/widgets/widgets-create-a-template) provided by the [Widget Provider](https://learn.microsoft.com/windows/apps/develop/widgets/widget-providers).

Widgets are rendered by Adaptive Cards, and there are a few ways Dev Home customizes the look and feel of the cards. Please note all of these are subject to change while Dev Home is in Preview.
* Dev Home widgets use the [Adaptive Card schema](https://adaptivecards.io/explorer/) version 1.5, which is the most recent schema supported by the WinUI 3 Adaptive Card renderer.
* There are [HostConfig](https://learn.microsoft.com/adaptive-cards/sdk/rendering-cards/uwp/host-config) files that define common styles (e.g., font family, font sizes, default spacing) and behaviors (e.g., max number of actions) for all the widgets. There is one for [light mode](../tools/Dashboard/DevHome.Dashboard/Assets/HostConfigLight.json) and one for [dark mode](../tools/Dashboard/DevHome.Dashboard/Assets/HostConfigDark.json).
* Dev Home supports a custom AdaptiveElement type called [`LabelGroup`](../common/Renderers/LabelGroup.cs). This allows a card author to render a set of labels, each with a specified background color. For an example of how to use this type, please see the [GitHub Issues widget](https://github.com/microsoft/devhomegithubextension/blob/main/src/GitHubExtension/Widgets/Templates/GitHubIssuesTemplate.json).
