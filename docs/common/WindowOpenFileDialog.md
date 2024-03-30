# Window open file dialog
Create a file dialog to open a single file

## Methods
| Method | Return | Description |
| -------- | ------ | -------- |
| ShowAsync(Window window) | StorageFile? | Show file dialog |
| AddFileType(string displayName, params string[] extensions) | void | Add a filter type on the dialog. Extension string should start with a '.' and at least one extension string must be provided. |

## Usage
#### Program.cs
```cs
using var fileDialog = new WindowOpenFileDialog();

fileDialog.AddFileType("YAML files", ".yaml", ".yml", ".winget");
fileDialog.AddFileType("Image files", ".png", ".gif");

StorageFile? file = await fileDialog.ShowAsync(window);
```
