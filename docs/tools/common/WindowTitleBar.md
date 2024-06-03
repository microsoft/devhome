# Window title bar
Create a window title bar with a customizable icon and title.

## Properties
| Property | Type | Description |
| -------- | -------- | -------- |
| Title | string | Window title bar title |
| Icon | IconElement | Window title bar icon |
| HideIcon | boolean | Hide the window title bar icon |
| IsActive | boolean | Indicate whether the title bar is active or not |

## Usage
#### MyPage.xaml
```xml
<Grid RowDefinitions="auto,*">
    <!-- Title bar -->
    <windows:WindowTitleBar x:Name="AppTitleBar" Title="My page" />
    
    <Grid Grid.Row="1">
        <!-- Content -->
    </Grid>
</StackPanel>
```
#### MyPage.xaml.cs
```cs
public MyPage(Window window)
{
    window.ExtendsContentIntoTitleBar = true;
    window.SetTitleBar(AppTitleBar);
    window.Activated += (_, args) => AppTitleBar.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
}
```