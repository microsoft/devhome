# ExtensionHost definition

DevHome is an app extension host with four possible extensions.  Plugins (app extensions) may connect
to any subset of extensions as needed.  Below is the appxmanifest code and description of each extension point. 

```xml
<uap3:Extension Category="windows.appExtensionHost">
  <uap3:AppExtensionHost>
    <uap3:Name>com.microsoft.DevHome.devdoctor</uap3:Name>
    <uap3:Name>com.microsoft.DevHome.settings</uap3:Name>
    <uap3:Name>com.microsoft.DevHome.setup</uap3:Name>
    <uap3:Name>com.microsoft.DevHome.widget</uap3:Name>
  </uap3:AppExtensionHost>
</uap3:Extension>
```

## devdoctor

Not Yet Implemented

Will enable a plugin to add rules and UI (via Adaptive Cards) to dev doctor.

## settings

Not Yet Implemented

Will enable a plugin to add UI through Adaptive Cards to the Dev Home settings page.

## setup

Not Yet Implemented

Will enable a plugin to add UI through Adaptive Cards to the Dev Home setup flow.

## widget

Not Yet Implemented

Will enable a plugin to add Widgets that can be added to the Dashboard.

# Runtime logic

At startup, Dev Home iterates the app catalog for any extensions and adds them to a dictionary.
```cs
private readonly IDictionary<string, IReadOnlyList<AppExtension>> _extensions = new Dictionary<string, IReadOnlyList<AppExtension>>();
```

Any internal tools can retrieve a readonly list of extensions for a given extension point via:
```cs
var extensionService = App.GetService<IExtensionService>();
var extensions = extensionService.GetExtensions("widget");
```

# Extensions definition

Here's an example of how a plugin can register to be a Dev Home extension:
```xml
<uap3:Extension Category="windows.appExtension">
  <uap3:AppExtension Name="com.microsoft.DevHome.widget"
    Id="ExampleId"
    PublicFolder="Public"
    DisplayName="ExampleName"
    Description="Example Description">
  </uap3:AppExtension>
</uap3:Extension>
```

# Extension Interface definions

Not Yet Implemented

These are being designed right now and will evolve over time before shipping.