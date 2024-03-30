# Window open folder dialog
Create a folder dialog to select a folder

## Methods
| Method | Return | Description |
| -------- | ------ | -------- |
| ShowAsync(Window window) | StorageFolder? | Show folder dialog |

## Usage
#### Program.cs
```cs
using var folderDialog = new WindowOpenFolderDialog();

StorageFolder? folder = await folderDialog.ShowAsync(window);
```
