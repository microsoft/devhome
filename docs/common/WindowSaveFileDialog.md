# Window save file dialog
Create a file dialog to save a file

## Methods
| Method | Return | Description |
| -------- | ------ | -------- |
| Show(Window window) | string? | Show file dialog |
| AddFileType(string displayName, params string[] extensions) | void | Add a 'Save As' file type on the dialog. Extension string should start with a '.' and at least one extension string must be provided. |

## Usage
#### Program.cs
```cs
using var fileDialog = new WindowSaveFileDialog();

fileDialog.AddFileType("YAML files", ".yaml", ".yml", ".winget");
fileDialog.AddFileType("Image files", ".png", ".gif");

var filePath = fileDialog.Show(window);
```
