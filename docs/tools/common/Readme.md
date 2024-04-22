# Common controls
List of common controls that are generic, customizable and reusable from all pages and tools in Dev Home.

## Windows
- [Window title bar](./WindowTitleBar.md)
- [Secondary window](./SecondaryWindow.md)

## Controls
- [CloseButton](./CloseButton.md)

## File dialog
> [!NOTE]
> File picker fails when running the application as admin.
>  To workaround this issue, we instead use the Win32 picking APIs
> as suggested in the documentation for the FileSavePicker:  
> _References: [Microsoft Learn](https://learn.microsoft.com/uwp/api/windows.storage.pickers.filesavepicker?view=winrt-22621#in-a-desktop-app-that-requires-elevation) - [Github issue](https://github.com/microsoft/WindowsAppSDK/issues/2504)_

- [WindowOpenFileDialog](./WindowOpenFileDialog.md)
- [WindowOpenFolderDialog](./WindowOpenFolderDialog.md)
- [WindowSaveFileDialog](./WindowSaveFileDialog.md)

## Custom Adaptive Card schema

Coming soon.
