# Window save file dialog
Create a file dialog to save a file

## Methods
| Method | Return | Description |
| -------- | ------ | -------- |
| Show(Window window) | string? | Shows the dialog and returns the file path selected by the user. Returns null if the user cancels the dialog. |
| AddFileType(string displayName, params string[] extensions) | IWindowFileDialogFilter | Adds a filter type on the dialog.<br>Extension string should start with a '.' and at least one extension string must be provided. |
| GetAvailableFileTypes() | IReadOnlyCollection&lt;IWindowFileDialogFilter&gt; | Gets the list of available file types |
| GetFileName() | string | Gets the file name |
| SetFileName(string value) | void | Sets the file name |
| GetFileType() | string | Gets the file type selected by the user |

## Usage
#### Program.cs
```cs
using var fileDialog = new WindowSaveFileDialog();

fileDialog.AddFileType("YAML files", ".yaml", ".yml", ".winget");
fileDialog.AddFileType("Image files", ".png", ".gif");

var filePath = fileDialog.Show(window);
```
