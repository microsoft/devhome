# Architecture

Dev Home has a modular architecture that consisting of multiple components. These are the major components of Dev Home: 

1. Dev Home Core
2. Dev Home Common
3. Settings
4. Tools
5. Extensions

![image info](images/architecture.png)

## Dev Home Core
Dev Home Core is the central part of Dev Home where all the components merge. It performs the following functionalities:

- Define the application packaging details
- Run the main application logic including managing the lifecycle, managing activations, creating the main window
- Create UI for the shell page and allow navigation among tools
- Find all the available tools and allow them to be displayed
- Start and manage the lifecycle of extensions

## Dev Home Common

The Dev Home Common component contains all the shared code among tools, Dev Home Core and the settings component. It also imports multiple libraries that are used across Dev Home. One such library is the Dev Home Extension SDK used to get references to out of process extensions.

Dev Home Common also provides logging and telemetry functionality to the application.

## Settings

This is a special component that acts similar to a Tool but isn't actually a tool. Settings component, similar to other tools consumes the Common project and is used by Dev Home core. It manages user preferences across all tools and extensions.

## Tools

The tools are a set of functionalities that are integrated within the app's codebase. These tools are designed to provide specific capabilities or features to the app. They live as their own component but in the same process as the app and can communicate with each other & the core component through the app's API.

These tools might get some required data/functionality from the extensions using the API in the extension SDK. 

Learn more about [writing a tool]()

Currently Dev Home has the following tools:

- [The Dashboard tool]()
- [The Setup Flow tool]()

## Extensions

The extensions are separate packages living as out-of-process components that provide functionality and data utilized by the core component & the tools. These extensions live outside the app's core codebase, and they interact with the app through a well-defined API or protocol.

The extensions can be developed by third-party developers or by the app's core development team. These extensions allow the app to be extended without modifying its core codebase.

Learn more about [writing an extension]()

Currently, we officially support the following extensions:

- [Dev Home GitHub extension](https://github.com/microsoft/devhomegithubextension)