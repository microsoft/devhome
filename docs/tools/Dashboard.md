# Dashboard

The Dashboard page hosts and displays Windows Widgets. Widgets are small UI containers that display text and graphics, associated with an app installed on the device. For information on creating widgets, see [Widgets Overview](https://learn.microsoft.com/windows/apps/design/widgets/) and [Widget providers](https://learn.microsoft.com/windows/apps/develop/widgets/widget-providers).

Each widget is represented by a [`WidgetViewModel`](../tools/Dashboard/DevHome.Dashboard/ViewModels/WidgetViewModel.cs). The WidgetViewModel displays itself inside a [`WidgetControl`](../../tools/Dashboard/DevHome.Dashboard/Controls/WidgetControl.xaml), and the WidgetControls are grouped into a [`WidgetBoard`](../../tools/Dashboard/DevHome.Dashboard/Controls/WidgetBoard.cs).

### Widget UI

The Widget UI consists of two main parts. At the top, there is a context menu and an attribution area. For more information on these components, please read [Built-in widget UI components](https://learn.microsoft.com/windows/apps/design/widgets/widgets-states-and-ui#built-in-widget-ui-components). The rest of the widget content is an [Adaptive Card](https://learn.microsoft.com/windows/apps/design/widgets/widgets-create-a-template) provided by the [Widget Provider](https://learn.microsoft.com/windows/apps/develop/widgets/widget-providers).

Widgets are rendered by Adaptive Cards, and there are a few ways Dev Home customizes the look and feel of the cards. Please note all of these are subject to change while Dev Home is in Preview.
* Dev Home widgets use the [Adaptive Card schema](https://adaptivecards.io/explorer/) version 1.5, which is the most recent schema supported by the WinUI 3 Adaptive Card renderer.
* There are [HostConfig](https://learn.microsoft.com/adaptive-cards/sdk/rendering-cards/uwp/host-config) files that define common styles (e.g., font family, font sizes, default spacing) and behaviors (e.g., max number of actions) for all the widgets. There is one for [light mode](../../tools/Dashboard/DevHome.Dashboard/Assets/HostConfigLight.json) and one for [dark mode](../tools/Dashboard/DevHome.Dashboard/Assets/HostConfigDark.json).
* Dev Home supports a custom AdaptiveElement type called [`LabelGroup`](../../common/Renderers/LabelGroup.cs). This allows a card author to render a set of labels, each with a specified background color. For an example of how to use this type, please see the [GitHub Issues widget](https://github.com/microsoft/devhomegithubextension/blob/main/src/GitHubExtension/Widgets/Templates/GitHubIssuesTemplate.json).

### Widget providers

When creating a widget for Dev Home, you should include all manifest values described in [Widget provider package manifest XML format](https://learn.microsoft.com/windows/apps/develop/widgets/widget-provider-manifest). Dev Home does not currently use `ProviderIcons`, but may choose to do so in the future, and including it will protect your widget provider from potentially breaking in the future.
