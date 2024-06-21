# Developer environments in Dev Home

"Environment" is the name Dev Home holistically gives a compute system and the resources it contains. These resources include apps, packages and cloned repositories. The environments page in Dev Home centralizes the experience for developers interacting with virtual or remote machines, such as local VMs, Cloud Dev Boxes and more!

## Terminology

**Compute system:** A compute system is considered to be any one of the following:

1. Local machine
2. Virtual machine
3. Remote machine
4. Container

Dev Home uses the [IComputeSystem](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L812) interface to interact with compute systems.

**Compute system provider:** A compute system provider is the provider type that Dev Home will query for when initially interacting with an extension. The compute system provider is used to perform general operations that are not specific to a ComputeSystem. Extension developers should implement the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L513) for their environment types.

**Adaptive cards:** [Adaptive cards](https://learn.microsoft.com/adaptive-cards/) are an open card exchange format enabling developers to exchange UI content in a common and consistent way.

## How to create an environment extension

### General Dev Home extension setup
If this is your first time building a Dev Home extension:
1. Follow general Dev Home [extension documentation](https://github.com/microsoft/devhome/blob/main/docs/extensions/readme.md) on how to get started with building an extension.
2. A sample Dev Home extension can be found [here.](https://github.com/microsoft/devhome/tree/main/extensions/SampleExtension)

### Environment specific setup

Your extension will need to do three things to be considered an extension that Dev Home recognizes as being an **environment** extension.

1. Within the `Package.appxmanifest` file for the package, the `<ComputeSystem />`  attribute should be added to the list of the extensions supported interfaces. To see an example of this, view Dev Home's [appxmanifest file for an example](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/src/Package.appxmanifest#L75).

2. You must create a class that implements the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L513) interface. This interface is used by Dev Home to perform specific operations like retrieving a list of [IComputeSystem](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L812) interfaces and creating a compute system.

3. Your extension must implement the [IExtension](https://github.com/microsoft/devhome/blob/3dc0dd739b0175357cc3e74c713d305c09248537/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L7) interface. The `GetProvider` method must return a single `IComputeSystemProvider` or a list of `IComputeSystemProvider`s.

### Environment extension tips

1. Try/catch any compute system related interface method that you implement. Since the calls are cross-process COM calls, the exception information will be lost once it bubbles over to Dev Home from the extension. Instead, return the appropriate result using its failure constructor.

2. When you decide to leave an interface method unimplemented, don't throw an exception. Instead for interface methods that return a runtime class, create the runtime class with the failure constructor and return it to Dev Home.

3. If the method has a `computerpart` value in the `ComputeSystemOperations` or `ComputeSystemProviderOperations` enum list, do not add the enum as a flag within the `IComputeSystem.SupportedOperations` and the `IComputeSystemProvider.SupportedOperations` properties. Dev Home uses the `SupportedOperations` property to determine which operations to show in the UI for the compute systems and the providers.

4. If a method is unimplemented and returns an interface, the method should return `null`. Dev Home will treat these as failures. However, you must adhere to tip #3 so Dev Home never calls this method.
