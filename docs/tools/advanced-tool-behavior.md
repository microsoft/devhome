# Advanced Tool behavior

## Headers

The default header when you create a Tool page is the string specified in `NavigationPane.Content` in [Creating a tool](./creating-a-tool.md).

### BreadcrumbBar headers

If you would like your page's header to be a [BreadcrumbBar](https://learn.microsoft.com/windows/apps/design/controls/breadcrumbbar), you can easily do this by using the `BreadcrumbBarDataTemplate`.

Add the following attributes to your page's View (.xaml file):
```xml
xmlns:behaviors="using:DevHome.Common.Behaviors"
behaviors:NavigationViewHeaderBehavior.HeaderTemplate="{StaticResource BreadcrumbBarDataTemplate}"
behaviors:NavigationViewHeaderBehavior.HeaderContext="{x:Bind ViewModel}"
```
In your page's ViewModel, create a  `Breacrumbs` property and populate it with as many [`Breadcrumb`](../../common/Models/Breadcrumb.cs)s as appropriate
```cs
public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

...

Breadcrumbs = new ObservableCollection<Breadcrumb>
{
    new(stringResource.GetLocalized("<first breadcrumb string>"), typeof(<first breadcrumb ViewModel>).FullName!),
    new(stringResource.GetLocalized("<second breadcrumb string>"), typeof(<second breacrumb ViewModel>).FullName!),
};
```

For a working example of this in Dev Home, see [AboutPage.xaml](../../settings/DevHome.Settings/Views/AboutPage.xaml) and [AboutViewModel.cs](../../settings/DevHome.Settings/ViewModels/AboutViewModel.cs).

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
