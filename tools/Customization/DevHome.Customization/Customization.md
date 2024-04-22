# Windows Customization

This directory contains the Windows Customization features for the DevHome project.

## Overview

The `DevHome.Customization` directory includes various features and settings that allow users to customize their Windows experience.


## Contributions

Windows Customization follows an MVVM (Model-View-ViewModel) architecture. The landing page, MainPage, is backed by a primary view, MainPageView. This serves as the first-level page for the overall Windows Customization feature set in DevHome, providing users with the ability to configure Windows settings to enhance their developer experience. The goal of this architecture is to ensure adaptability of the user interface.

MainPage hosts links to other pages via SettingsCard controls. These pages contain logically grouped settings and features that can be configured by the user. Each grouping is designed as portable controls, such as a UserControl. This design allows them to be hosted on the main page, included in a future "All Settings" page, and be part of a future filtered search results page.

New feature additions to should expect to implement the following:

1. **View**: Generally a StackPanel that contains all the user interface for the new feature or setting(s). This View should generally be implemented as a UserControl that can be hosted in any number of Pages.
    - e.g. [FileExplorerView](Views\FileExplorerView.xaml)

1. **ViewModel**: Generally the implementation of data binding and commands needed to support the View, including notifications to trigger changes in the View's UI.
    - e.g. [FileExplorerViewModel](ViewModels\FileExplorerViewModel.cs)

1. **Page**: Optionally include a page that hosts the View as a navigation target from the [MainPage](Views\MainPage.xaml). This may not be needed if the settings are minimal and can be hosted on the [MainPageView](Views\MainPageView.xaml) (e.g., as a SettingsExpander).
    - e.g. [FileExplorerPage](Views\FileExplorerPage.xaml)

1. **Model**: Any implementation details for the setting or feature, i.e. the data model and any business or validation logic.
    - e.g. [FileExplorerSettings](Models\FileExplorerSettings.cs)

1. **Service entries**: Pages, ViewModels, and any other services participating in dependency injection should be placed in AddWindowsCustomization in [ServiceExtensions.cs](Extensions\ServiceExtensions.cs)

1. **Strings**: Localized strings should leverage [Resources.resw](Strings\en-us\Resources.resw)

1. **Telemetry Events**: Any user interaction that results in a settings change should use [SettingChangedEvent](TelemetryEvents\SettingChangedEvent.cs)

> **Note**: Private APIs (including registry keys or values that are not documented) cannot be referenced in the Windows Customization project. For public contributions that involve a private API, please file an issue on GitHub to discuss options.
