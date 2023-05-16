# Tools

Dev Home adds functionality through a set of tools. Each tool provides a page in the Dev Home navigation view. Currently, all tools come from in-app assemblies. Third party or out-of-process tools are not supported at this time.

Tools utilize data and functionality from out-of-process [extensions](./extensions.md). This is done through the Extension SDK API. 

## Writing a Tool

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
  - [`ShortName`](#ShortName) property to get the name of the tool

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

## Setup Flow Tool
