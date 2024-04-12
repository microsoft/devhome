# Close button
Create a close button (X button) for exiting a piece of UI.

## Usage
### Example 1: Use the CloseButton in fixed-width UI
#### HelloWorldDialog.xaml
```xml
<!-- Title and Close button -->
<Grid>
    <TextBlock Text="Dialog Title" HorizontalAlignment="Left" />
    <commonviews:CloseButton Command="{x:Bind CancelButtonClickCommand}" />
</Grid>
```

#### HelloWorldDialogViewModel.cs
```cs
[RelayCommand]
private void CancelButtonClick()
{
    HideDialogAsync();
}
```

### Example 2: Use the CloseButton in dynamically-sized UI, preventing overlap when the width is small
#### HelloWorldControl.xaml.cs
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="auto" />
    </Grid.ColumnDefinitions>

    <!-- Title -->
    <TextBlock Text="Control Title" />

    <!-- Close button -->
    <common:CloseButton
        Grid.Column="1"
        Command="{x:Bind CancelButtonClickCommand}" />
</Grid>
```
