# Dev Environments in Dev Home

Dev Environments is the name Dev Home holistically gives a compute system and the projects it contains. These projects can contain apps, packages and cloned repositories. The goal of Dev Environments within Dev Home is for developers to have a single place where they can easily switch between all their development related workflows.

## Terminology

**Compute System:** A compute system is considered to be any one of the following:

1. Local machine
1. Virtual machine
1. Remote machine
1. Container

Dev Home uses the [IComputeSystem](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L757) interface to interact with these types of software/hardware systems.

**Compute system provider:** A compute system provider is the provider type that Dev Home will query for when initially interacting with an extension. The compute system provider is used to perform general operations that are not specific to a ComputeSystem. Extension developers should implement the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L458).

## What is needed to create a Dev Environments extension?

First take a look at Dev Homes extensions documentation [here.](https://github.com/microsoft/devhome/blob/main/docs/extensions.md)
Then see Dev Homes sample extension [here.](https://github.com/microsoft/devhome/tree/main/extensions/SampleExtension)

Your extension will need to do three things to be considered an Extension that Dev Home recognizes as being a Dev Environment extension.

1. Within the `Package.appxmanifest` file for the package, the `<ComputeSystem />`  attribute should be added to the list of the extensions supported interfaces. See: Dev Home's [appxmanifest file for an example](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/src/Package.appxmanifest#L75)

1. A class that implements the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L458) interface should be created. This interface is used by Dev Home to perform specific operations like retrieving a list of [IComputeSystem](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L757) interfaces and creating a compute system.

1. Your extension should implement the [IExtension](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L7) interface and within its `GetProvider` method return a single `IComputeSystemProvider` or a list of `IComputeSystemProvider`s.

### Recommendations

1. We recommend to try/catch any compute system relate interface method that you implement as the calls are cross process COM calls. This means that the exception information will be lost once it bubbles over to Dev Home from the extension. Instead, return the appropriate result using its failure constructor. See the Examples below for how this can be done.

1. When you decide to leave an interface method unimplemented, don't throw an exception. Instead for interface methods that return a runtime class, create the runtime class with the failure constructor and return it to Dev Home.

1. If the method has a computerpart value in the `ComputeSystemOperations` or `ComputeSystemProviderOperations` enum list, do not add the enum as a flag within the `IComputeSystem.SupportedOperations` and the `IComputeSystemProvider.SupportedOperations` properties. Dev Home uses the supported operations property to know which operations to show in the UI for the compute systems and the providers.

1. If a method is unimplemented and returns an interface, the method should return null. Dev Home will treat these as failures. However, you should still adhere to recommendation 3 above so Dev Home never calls this method.
