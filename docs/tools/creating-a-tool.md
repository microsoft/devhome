# Creating a tool

1. Create a new directory with your tool's name under `tools` with three subdirectories `src`, `test`, and `uitest`
1. Create a new `WinUI 3 Class Library` project in your `src` directory
1. In your project file, remove `TargetFramework` and `TargetPlatformMinVersion`. Add the following line to the top:
    ```xml
    <Import Project="$(SolutionDir)ToolingVersions.props" />
    ```
1. Remove the PackageReference to WindowsAppSDK, since it will be added via the Common project in a few steps.
1. Create the `Strings\en-us` directories under `src`. Add `Resources.resw` and include the following code:
    ```xml
    <data name="NavigationPane.Content" xml:space="preserve">
      <value>[Name of your tool that will appear in navigation menu]</value>
      <comment>[Extra information about the name of your tool that may help translation]</comment>
    </data>
    ```
1. Add a project reference from `DevHome.csproj` to your project
1. Add a project reference from your project to `DevHome.Common.csproj` project under [/common/](/common)
1. Create your XAML View and ViewModel. Your View class must inherit from `ToolPage` and implement [tool interface requirements](#tool-requirements).
1. Update [NavConfig.jsonc](/src/NavConfig.jsonc) with your tool. Specifications for the [NavConfig.json schema](./navconfig.md).

## Tool requirements

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

## Code organization

[`toolpage.cs`](../common/ToolPage.cs)
Contains the interface definition for Dev Home tools.