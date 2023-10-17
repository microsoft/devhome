# Experimental features

Dev Home supports "Experimental features" - these are features that are disabled by default, but can be enabled by the user. 
This is useful for features that are not ready for general use, but can be tested by users who are interested in trying them out and providing feedback to inform the development of the feature.


## Adding a new experimental feature

1. Add a new string in the `experiments` array in `navconfig.jsonc`, e.g. MyExperimentalFeature
2. Add Name and Description strings under settings/DevHome.Settings/Strings/en-us/Resources.resw:
```xml
  <data name="MyExperimentalFeature_Description" xml:space="preserve">
    <value>Some description</value>
  </data>
  <data name="MyExperimentalFeature_Name" xml:space="preserve">
    <value>My experimental feature's name</value>
  </data>
```

## Making a tool page visible only when the feature is enabled

In `navConfig.jsonc`, add the following in the tool's definition:
`"experimentId": "MyExperimentalFeature"`

## Checking if a feature is enabled

```csharp
var experimentalFeaturesViewModel = App.Current.GetService<ExperimentalFeaturesViewModel>();
var isEnabled = experimentalFeaturesViewModel.Features.Where(x => string.Equals(x.Id, "MyExperimentalFeature", StringComparison.OrdinalIgnoreCase).First().IsEnabled;
```
