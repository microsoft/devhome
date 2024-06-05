# Secondary window
Create a secondary application window that derives from `WinUIEx.WindowEx` ensuring a consistent look and feel as the main window.

## Additional properties
| Property | Type | Description |
| -------- | -------- | -------- |
| WindowTitleBar | WindowTitleBar (_*null_) | Secondary window title bar |
| UseAppTheme | Boolean (_*true_) | Use the app main window theme and react to theme changes |
| IsModal | Boolean (_*false_) | Disable interaction with the primary window until the secondary window is closed |
| IsTopLevel | Boolean (_*false_) | Keep the secondary window on top of the primary window |

\* Default value

## Additional methods
| Property | Retrun type | Description |
| -------- | -------- | -------- |
| CenterOnWindow() | void | If the primary window is set, center the secondary window on the primary window. Otherwise, center the secondary window on the screen by calling `WindowExExtensions.CenterOnScreen()`. |

## Usage
### Example 1: Set content from XAML
#### HelloWorldWindow.xaml.cs
```xml
<windows:SecondaryWindow
    x:Class="DevHome.Tools.Example.HelloWorldWindow"
    ...
    IsModal="True"
    UseAppTheme="False"
    IsTopLevel="True">

    <!-- Customize the secondary window title bar -->
    <windows:SecondaryWindow.WindowTitleBar>
        <windows:WindowTitleBar Title="Hello world title" />
    </windows:SecondaryWindow.WindowTitleBar>

    <!-- Set secondary window content from XAML -->
    <TextBlock Text="Hello world from XAML" />
</windows:SecondaryWindow>
```

#### HelloWorldWindow.xaml.cs
```cs
public sealed partial class HelloWorldWindow : SecondaryWindow
{
    public HelloWorldWindow()
    {
        this.InitializeComponent();
    }    
}
```

#### MainWindow.cs
```cs
public void OpenSecondaryWindow()
{
    var secondaryWindow = new HelloWorldWindow();
    secondaryWindow.Activate();
    secondaryWindow.CenterOnWindow();
}
```

### Example 2: Set content from C#
#### HelloWorldWindow.xaml.cs
```xml
<windows:SecondaryWindow
    x:Class="DevHome.Tools.Example.HelloWorldWindow"
    ...
    IsModal="True"
    UseAppTheme="False"
    IsTopLevel="True">

    <!-- Customize the secondary window title bar -->
    <windows:SecondaryWindow.WindowTitleBar>
        <windows:WindowTitleBar Title="Hello world title" />
    </windows:SecondaryWindow.WindowTitleBar>

    <!-- Set secondary window content from C# code-behind -->
</windows:SecondaryWindow>
```

#### HelloWorldWindow.xaml.cs
```cs
public sealed partial class HelloWorldWindow : SecondaryWindow
{
    public HelloWorldWindow(string text)
        : base(new TextBlock { Text = text })
    {
        this.InitializeComponent();
    }    
}
```

#### MainWindow.cs
```cs
public void OpenSecondaryWindow()
{
    var secondaryWindow = new HelloWorldWindow("Hello world from C#");
    secondaryWindow.Activate();
    secondaryWindow.CenterOnWindow();
}
```
