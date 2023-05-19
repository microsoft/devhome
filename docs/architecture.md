# Architecture

Dev Home has a modular architecture that consists of multiple components. These components are:

1. Dev Home Core
2. Dev Home Common
3. Settings
4. Tools
5. Extensions

![image info](images/architecture.png)

## Dev Home Core

Dev Home Core is the central part of Dev Home where all the components come together. The functions it performs include:

- Defining the application packaging details
- Running the main application logic including managing lifecycle, managing activations, and creating the main window
- Creating UI for the shell page and allowing navigation among tools
- Finding all the available tools and allowing them to be displayed
- Starting and managing the lifecycle of extensions

## Dev Home Common

The Dev Home Common component contains code that is shared among the tools, core, and settings component. It also imports libraries that are used across Dev Home. One such library is the **Dev Home Extension SDK** used to get references to out-of-process extensions.

Dev Home Common also provides logging and telemetry functionality to the application.

## Settings

This is a special component that acts similarly a tool but isn't actually a tool. The Settings component, like other tools, consumes the Common project and is used by Dev Home Core. It manages user preferences across all tools and extensions.

## Tools

The tools are a set of functionalities that are integrated within the app's codebase. They are designed to provide specific capabilities or features to the app. They live as their own component but run in the same process as the app and can communicate with each other and the core component through the app's API.

These tools can use the APIs in the extension SDK to get data or functionality from the extensions.

Learn more about [writing a tool](./tools.md).

Dev Home currently has the following tools:

- [Dashboard](./tools.md#dashboard-tool)
- [Setup flow](./tools.md#setup-flow-tool)

## Extensions

Extensions are separate packages living as out-of-process components that provide functionality and data used by the Core component and the tools. These extensions live outside of the app's core codebase, and they interact with the app through a well-defined API or protocol.

Extensions can be developed by third-party developers or by the app's core development team. These extensions allow the app to be extended without modifying its core codebase.

Learn more about [writing an extension](./extensions.md).
