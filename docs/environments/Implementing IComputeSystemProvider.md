# What are Dev Environments in Dev Home

Dev Environments is the name Dev Home holistically gives a compute system which can contain dev projects. These projects contain apps, packages and cloned repositories. The goal of Dev Environments is for developers to have a one stop shop for all their environments in a single place.

## What is a Compute System?

A compute system is considered to be any of the following: virtual machine, remote machine or a container. Dev Home uses an [IComputeSystem](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L757)

## What is needed to create a Dev Environment extension?

First take a look at Dev Homes extensions documentation [here](https://github.com/microsoft/devhome/blob/main/docs/extensions.md)
Then see the sample extension documentation Dev Homes sample extension [here](https://github.com/microsoft/devhome/tree/main/extensions/SampleExtension)

Your extension will need to do three things to be considered an Extension that Dev Home recognizes as being a Dev Environment extension.

1. Within the `Package.appxmanifest` file for the package, the `<ComputeSystem />`  attribute should be added to the list of the extensions supported interfaces. See: Dev Home's [appxmanifest file](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/src/Package.appxmanifest#L75)

1. Your extension should implement the [IExtension](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L7) interface and within its `GetProvider` method return a single `IComputeSystemProvider` or a list of `IComputeSystemProvider`s

1. A class that implements the [IComputeSystemProvider](https://github.com/microsoft/devhome/blob/1fbd2c1375846b949dd3cc03b2553b8b8efa1f64/extensionsdk/Microsoft.Windows.DevHome.SDK/Microsoft.Windows.DevHome.SDK.idl#L458) interface should be created. This interface is used by Dev Home to perform specific operations like retrieving a list of [IComputeSystems]()and creating a compute system.

## What should an IComputeSystemProvider 
