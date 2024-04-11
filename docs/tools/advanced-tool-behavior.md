# Advanced Tool behavior

## Headers

Coming soon.

## Navigation

In order to allow navigation to your tool, your page and ViewModel must be registered with the PageService. If your tool only contains one page, it is automatically registered for you since you added your page to `NavConfig.jsonc`. However, you may have other sub-pages you wish to register.

In order to do so, you must create an extension method for the PageService inside your tool. See examples in [Settings](../settings/DevHome.Settings/Extensions/PageExtensions.cs) or [Extensions](../tools/ExtensionLibrary/DevHome.ExtensionLibrary/Extensions/PageExtensions.cs). Then, call your extension from the [PageService](../src/Services/PageService.cs).

### Navigating away from your tool

If you want a user action (such as clicking a link) to navigate away from your tool and your project does not otherwise rely on the destination project, do not create a PackageReference to the destination project from yours. Instead, add the destination page to `INavigationService.KnownPageKeys` and use it for navigation, like in this example:
```cs
navigationService.NavigateTo(KnownPageKeys.<DestinationPage>);
```

This keeps the dependency tree simple, and prevents circular references when two projects want to navigate to each other.

## Common controls

Dev Home has a set of common controls that are generic, customizable and reusable from all pages and tools in Dev Home. See [Common controls](./common/Readme.md).