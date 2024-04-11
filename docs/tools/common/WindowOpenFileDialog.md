# Window open file dialog
Create a file dialog to open a single file

## Properties
| Property | Return | Description |
| -------- | ------ | -------- |
| FileName | string | Gets or sets the file name |
| FileType | string | Gets the file type selected by the user |
| AvailableFileTypes | IReadOnlyCollection&lt;IWindowFileDialogFilter&gt; | Gets the list of available file types |

## Methods
| Method | Return | Description |
| -------- | ------ | -------- |
| ShowAsync(Window window) | StorageFile? | Shows the dialog and returns the selected file. Returns null if the user cancels the dialog. |
| AddFileType(string displayName, params string[] extensions) | IWindowFileDialogFilter | Adds a filter type on the dialog.<br>Extension string should start with a '.' and at least one extension string must be provided. |
| RemoveFileType(IWindowFileDialogFilter filter) | bool | Removes a filter type from the dialog. |

## Usage
#### Program.cs
```cs
using var fileDialog = new WindowOpenFileDialog();

fileDialog.AddFileType("YAML files", ".yaml", ".yml", ".winget");
fileDialog.AddFileType("Image files", ".png", ".gif");

StorageFile? file = await fileDialog.ShowAsync(window);
```
