# Localized strings

All strings shown to the user must be localized. The `StringResource` service provides an easy way to access those strings.

## Usage

### Creating a localized string

The project you are working in should have a directory under `Strings\en-us`, containing a file called `Resources.resw`.
If it does not, see [Writing a tool](../tools.md#writing-a-tool).
For information about adding strings to this file, refer to 
[Store strings in a resources file](https://learn.microsoft.com/windows/apps/windows-app-sdk/mrtcore/localize-strings#store-strings-in-a-resources-file).
Dev Home will take care of getting localized versions of these strings.

### Referencing your localized string from XAML

To learn how to reference your string from XAML, see 
[Refer to a string resource identifier from XAML](https://learn.microsoft.com/windows/apps/windows-app-sdk/mrtcore/localize-strings#refer-to-a-string-resource-identifier-from-xaml).

### Referencing your localized string from code

While it is possible to reference your strings from code using the method in the article linked to on this page, for consistency you should use the `StringResource` service.
You can obtain a reference to the StringResource service with the following code:
```cs
var stringResource = new StringResource("<your_project_name>.pri", "<your_project_name>/Resources");
```
For example, the DevHome.Settings project would use
```cs
var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
```

Then, to get a specific string, use `StringResource.GetLocalized()`.
```cs
// Here, MyString is the "Name" of the string to be localized that you specified in the Resources.resw file
var str = stringResource.GetLocalized("MyString");
```
