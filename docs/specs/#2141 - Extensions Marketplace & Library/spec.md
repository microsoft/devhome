---
author: Ethan Fang @ethan-fang-MS/ethanfang@microsoft.com
created on: 2024-02-26
last updated: 2024-02-26
issue id: #2141
---

# Dev Home Extensions Marketplace & Library

## 1. Overview

### 1.1 Establish the Problem

Dev Home supercharges the developer experience and is built to allow developers to customize their experience and take advantage of the community we anticipate will surround the application. Part of this experience includes first and third-party-built extensions that extend, enhance, and amplify the Dev Home experience. Simply put, this requires architecture to support third-party developers in developing extensions: 
- Connecting users to the marketplace of published extensions 
- Providing a mechanism for managing and updating the library of currently installed extensions 

### 1.2 Introduce the Solution

We believe that Dev Home has the potential to become the go-to developer surface for Windows, and we want to make it even better by providing a platform for developers to easily discover and manage extensions that enhance the user experience within Dev Home. By building an extensions marketplace directly within Dev Home, we can help streamline the process of finding and managing extensions, making it easier for developers to customize their experience with the application and improve their productivity. We also hope to promote the future development of extensions by third parties by (1) broadcasting the existence of extensions with a built-in page in Dev Home, (2) demonstrating our commitment to helping extensions developers reach the end users on Dev Home. 

### 1.3 Rough-in Designs

![Extensions Marketplace](./image1-Extensions%20Marketplace.png)

![Extensions Library](./image2-Extensions%20Library.png)


## 2. Goals & User Cans

### 2.1 Goals
1. Provide easy access to the install and management of extensions in Dev Home 

2. Make extensions more visible to Dev Home users 

3. Incentivize third-party extensions with a dedicated surface to showcase extensions 


### 2.2 Non-Goals
1. Explicitly handle the install of extensions from the local file system (this should automatically be recognized – no need for an installation flow in Dev Home) 

2. Handle the payment processing for paid extensions (instead direct users to complete installation in the Microsoft Store) 

3. Provide a surface for creating/uploading newly created extensions to the MSFT store 


### 2.3 User Cans Summary Table

| No. | User Cans | Priority |
|-----|-----------|----------|
| 1   | Users can install extensions from the Microsoft Store, directly in Dev Home, or from their local file system | 0 |
| 2   | Users can browse for extensions directly in Dev Home by direct search, publisher, recommendations, number of downloads, & top rated | 0 |
| 3   | Users can manage their installed extensions (check for updates, update extensions, view information about their extensions, remove extensions) | 0 |
| 4   | Organizations can manage extensions page & flows (GPO, install, update, remove, block) in Dev Home | 0 |

## 3. User Stories

### 3.1 User story - Searching for Extensions

#### Job-to-be-done
User wants to search for an extension that works with Dev Home

#### User experience
1. User navigates to the Extensions Marketplace page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Marketplace” s\

2. User is presented with a default list of extensions organized by “Recommended for you”, “Top Downloads”, etc. 

3. Currently installed extensions that appear are clearly marked as installed 

    - Users glances through the page making note of the shown extensions to see the extensions they are looking for appears there (if it does, they can skip the next step) 

4. User clicks on the search box and starts typing the name of the extension they are looking for 

5. Search results populate in real time 

6. User sees the extension they wish to view and clicks “Learn More” in the row listing, or anywhere on the extension tile to view more information about the extension 

#### Golden paths
Users browsing for extensions or looking for a specific extension can turn to the Extensions Marketplace page. In a browsing experience, Dev Home automatically populates the marketplace with several carousels of extensions group by: 

- Recommended for you 

- Top downloads 

- Top rated 

- New 

- Etc. 

Glancing at each extension from this view should inform users about the listed extensions including: 

- Name 

- Logo 

- Publisher 

- Installed, Disabled, or Not Installed (Shown as “Installed”, “Disabled”, or “Install” respectively on the tile) 

- Rating 

- Number of Downloads 

 

In addition to a browsing experience, users can also search directly for extensions using the search bar for queries – this appears as the view picture on the right in the diagram above. In this view, users see the following information on each listed extension: 

- Name 

- Version? 

- Publisher 

- Installed or Not Installed 


### 3.2 User story - Viewing Extensions

#### Job-to-be-done
User wants to understand more about a Dev Home extension prior to installing the extension

#### User experience
1. User navigates to the Extensions Marketplace page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Marketplace” 

2. Users glances through the page making note of the shown extensions to see the extensions they are looking for appears there (if it does, they can skip the next step) 

3. User clicks on the search box and starts typing the name of the extension they are looking for – search results populate in real time 

4. User sees the extension they wish to view and clicks anywhere on the extension tile to view more information about the extension 

5. User can click “View in Microsoft Store” to view the extension as listed as an application in the Microsoft Store – this will open up given extension’s listing in the Microsoft Store application  

#### Golden paths 
To learn more about listed (or searched) extensions, users can click on the tile for a given extension. This should invoke a modal that users can easily exit by clicking away or pressing “Esc”. The modal gives additional information and actions including: 

- Description 

- Screenshots 

- Additional Information 

    - Release Date 

    - Approximate Size 

    - Link to Terms 

- Permissions 

- Link to view in the Microsoft Store 

This information should be consistent with the extension’s Microsoft Store listing. 

 

For extensions that are already installed on the user’s PC, the “Install” text is replaced with “Installed” in the browse view and in the pop-up modal, the “Install” text instead reads “View in Library”. 

### 3.3 User story - Installing Extensions

#### Job-to-be-done 
(1) User wants to install a **free** extension that works with Dev Home

(2) User wants to install a **paid** extension that works with Dev Home

#### User experience
Installing a **free** extension:
1. User navigates to the Extensions Marketplace page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Marketplace” 

2. Users follows the “viewing/searching for extensions “user-path to arrive at a free extension they wish to install 

3. User clicks on the “Install” button – the extension then starts installing as the “Install” button shows an installation progress circle with the label “Installing…” – once installation completes, this button now reads “View in Library” 

4. User can confirm successful installation by clicking on the “View in Library” button – this navigates to the “Extensions Library” page within Dev Home 

5. User can see the newly installed extension listed under the “On your PC” section 

Installing a **paid** extension:
1. User navigates to the Extensions Marketplace page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Marketplace” 

2. Users follows the “viewing/searching for extensions” user-path to arrive at a paid extension they wish to install 

3. User clicks on the “Buy” button (note: this replaces the “Install” text if an extensions is paid) – this opens the extension in the Microsoft Store app 

4. User clicks on the button with the price (i.e. $0.99) in the top right of the Microsoft Store listing and follows prompts to complete payment and start installation 

5. User can confirm successful installation by clicking on the “View in Library” button back on the Extensions Marketplace page in Dev Home – this navigates to the “Extensions Library” page within Dev Home – or simply navigate to the Extensions Library themselves by by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Library” 

6. User can see the newly installed extension listed under the “On your PC” section 

#### Golden paths 
To install an extension from the built-in Extensions Marketplace in Dev Home, users simply click “Install”.  

- **Free**: If the extensions is “Free”, installation is contained within Dev Home and the user can see installation progress live in the Extensions Marketplace.  

- **Paid**: If the extensions is a “Paid” store experience, then installation directs to the Microsoft Store, where users can complete payment and installation within the Microsoft Store. Dev Home should automatically recognize that a compatible extension has been installed and show this new extension in the Extensions Library. 

Installations may require Dev Home or the device to restart – if this is the case, the Extensions Library should show that this action is required and guide users to take the needed actions for changes to be reflected.  

### 3.4 User story - Updating Extensions

#### Job-to-be-done
User wants to make sure their extensions are up to date

#### User experience
1. User navigates to the Extensions Library page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Library” 

2. User clicks “Check for updates” – this refreshes the list of extensions that have updates to be applied  

3. User clicks “Update” on one of the extensions listed in the “Updates” section – update process starts and the “Update” text is replaces by a progress circle – once the update complete, the extension no longer appears as a line item within the “Updates” section 

4. User can view the extension within the “On your PC” section and see the “Last updated” date showing as “Now” 

#### Golden paths 
Users can manage all of their Dev Home extensions from the Extensions Library page. Management includes the ability to:
- Check for updates
- Update extensions
- View & take needed actions (e.g., an extension "Needs Restart")
- Delete extensions
- View information about installed extensions, including:
    - Publisher
    - Version
    - Size
    - Date of last update/installation
    - Installation method
    - Disabled?

- Users can also sort extensions in their library by:
    - Name
    - Publisher
    - Version
    - Size
    - Date of last update/installation
    - Installation method


### 3.5 User story - Removing/Turn-off Extensions

#### Job-to-be-done
(1) User wants to uninstall a Dev Home extension

(2) User wants to disable/turn off a Dev Home extension

#### User experience
**Uninstalling** Extensions:
1. User navigates to the Extensions Library page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Library” 

2. User clicks on the three dots UI which opens a context menu 

3. User clicks “Uninstall” to remove the extension – this opens a modal asking the user “Uninstall [Extension]?” with subtext that says “Disable the extension instead by going to Dev Home Settings > [Extension] > Disable” 

4. User clicks “Yes” – Dev Home opens up the settings app to the uninstall page with the extension item pulled up 

5. User can confirm that the uninstall was successful because extension no longer appears in the Extensions Library 

**Disabling** Extensions:
1. User navigates to the Extensions Library page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Library” 

2. User clicks on the three dots UI which opens a context menu 

3. User clicks “Disable” to disable the extension  

4. Extension now appears as greyed out (user can undo this by right-clickin and hitting “Enable”) 

5. User can confirm that the disable was successful because extension appears as greyed out and has a “Disabled” text label on the row item 

#### Golden paths 
To remove or disable/enable extensions, users click the three dots (or right clicking) on a given extension, and select the option to “Uninstall” or “Disable”/”Enable” (the “Other Options” are written by the extension developer). This prompts a modal to confirm the choice, which if the user affirms, will remove the extension from the device.  

Disabling an extension will not uninstall the app that the extension ships with, and instead block the extension from interacting with Dev Home (but the extension should still appear in the library and marketplace). This can be changed back at any time by navigating to the settings for the extension or right-clicking and hitting “enable” on the extension within the Extensions Library. 

Users should also be able to uninstall extensions via the Microsoft Store or OS Settings app like they would for any normal application and this action should update the Extensions Library in Dev Home. 


### 3.6 User story - Sorting Extensions

#### Job-to-be-done
User wants to sort their extensions in the Extensions Library to learn more about their installed extensions 

#### User experience
1. User navigates to the Extensions Library page within Dev Home by clicking the “Extensions” icon/title in the left-hand navigation page and using the drop-down to click on “Extensions Library” 

2. User clicks on the “Sort by” drop-down 

3. User can click on any of the following sort options for the sort to be reflected in the view: 

    - Name 

    - Publisher 

    - Version 

    - Size 

    - Date of last update/installation 

    - Installation method 

4. User can click away from the drop-down and the last clicked sort option will persist 

#### Golden paths 
As mentioned above, user can sort their extensions by: 

- Name 

- Publisher 

- Version 

- Size 

- Date of last update/installation 

- Installation method 

## 4. Requirements

### 4.1 Functional Requirements

#### Summary

In order to strategically implement the Extensions feature within Dev Home, we have devised a crawl, walk, run staging plan. This approach is rooted in the necessity to deliver immediate value while ensuring a robust and scalable solution for the future. The crawl stage, our initial focus, involves developing a singular page encompassing both the marketplace and library aspects of the extension experience. This page will meet legal requirements and provide the minimum feature-set required to engage early adopters and stimulate user growth. Following the crawl stage, the walk phase will involve refining and expanding this foundation, enhancing user interactions, and addressing initial feedback. Finally, the run stage will see us fully embracing the vision, enriching the Dev Home experience with comprehensive extension management & discovery, and fostering a thriving developer community.  

Staging Plan:
1. **Crawl**: The Foundation Extension Experience

    - The Crawl stage of the Extensions implementation focuses on delivering the foundational elements necessary to introduce the Extensions feature within Dev Home. At this stage, our primary goal is to create a single integrated page that combines both the marketplace and library aspects of the extension experience. This page will provide users with access to the marketplace featuring published extensions and enable them to both install and then manage and update their installed extensions from the library. The emphasis is on meeting legal requirements (DMA), establishing a basic infrastructure, and providing early adopters with a glimpse of the upcoming capabilities. By concentrating on these core functionalities, we ensure compliance while also offering a simple yet functional starting point that lays the groundwork for subsequent stages of refinement and expansion. 
2. **Walk**: Expanding and Enhancing Extension Engagement

    - The walk stage acts as a pivotal bridge between the foundational crawl stage and the comprehensive run stage of the Extensions implementation. It focuses on enriching user interactions and introducing select features that enhance the Dev Home extension experience while maintaining a balanced evolution. 
3. **Run**: Robust Extensions Marketplace & Library Experience

    - The run stage represents the pinnacle of the Extensions feature within Dev Home, embodying a fully realized vision of user customization and community engagement. With two distinct pages — the Extensions Marketplace and the Extensions Library — this stage delivers a comprehensive extension experience that empowers users to personalize their Dev Home environment and cultivates a thriving developer community. 

#### Detailed Experience Walkthrough

![Extensions Marketplace](./image3-Labeled%20Extensions%20Marketplace.png)

![Extensions Marketplace Pt. 2](./image4-Labeled%20Extensions%20Marketplace%20Pt2.png)

![Extensions Library](./image5-Labeled%20Extensions%20Library.png)

#### Detailed Functional Requirements

| No. | Requirement | Pri |
|-----|-------------|-----|
| 1.1 | (Extensions installed via Microsoft Store) Dev Home recognizes when extensions are installed from the Microsoft Store and takes appropriate actions so that extensions are usable inside Dev Home [This currently happens by default] | 0 |
| 1.2 | (Installing extensions via Dev Home) Dev Home provides an extensions marketplace page that can be used to complete the extension installation process from start to finish | 0 |
| 1.3 | (Installing extensions via Local File System) Dev Home recognizes when extensions are installed from the local file system and takes appropriate actions so that extensions are usable inside Dev Home | 0 |
|     | Note: Non-Goal – Explicitly handle the install of extensions from the local file system (this should automatically be recognized – no need for a local installation flow in Dev Home) |   |
| 1.4 | If an extension requires an OS restart in order for changes to deploy, Dev Home needs to be aware of this and communicate this need to the user (i.e., an example is WSL having been installed via docker) | 0 |
|     | Note: We expect most extensions to not require a restart in order for changes to be reflected on the user’s PC and in Dev Home, see Requirement 1.5 |   |
| 1.5 | Dev Home successfully handles updates to extensions such that the Dev Home app doesn’t need a restart for changes to take effect | 0 |
|     | Note: Exception being OS restarts, see Requirement 1.4 |   |
| 1.6 | For paid extensions, installation via the extensions marketplace directs users to process installation of paid extensions in the Microsoft Store (see User Stories > 3.1 Marketplace > Installing Extensions above) | 0 |
| 2.1 | Dev Home has a built-in Extensions Marketplace page | 0 |
|     | Note: Also captured in Requirement 1.2 |   |
| 2.2 | The extensions marketplace displays all of the Dev Home compatible extensions listed in the Microsoft Store | 0 |
| 2.3 | The extensions marketplace page provides a search box for users to browse all of the Dev Home compatible extensions published in the Microsoft Store | 2 |
| 2.4 | The extensions marketplace page search box can take in specific query filter inputs including “publisher: [ ]” and filter search results by the specific query | 3 |
| 2.5 | The extensions marketplace page is pre-populated with carousels of extensions (shown as tiles – defined in Requirement 2.6) by the following categories: | 2
|   |    - Recommended for the user
|   |    - Top downloads
|   |    - Top-rated
|   |    - New
| 2.6 | The extensions marketplace page provides information about each extension shown as a tile on the Extensions Marketplace landing page, including: | 1 
|   |    - Name
|   |    - Logo
|   |    - Publisher
|   |    - Installed/Disabled/Not Yet Installed
|   |    - Store Rating
|   |    - Number of Downloads
|   |    - Text button to “Learn more” 
| 2.7 | Clicking on a given “extension tile” or the “Learn more” text button within the extension tile opens a modal with expanded and additional information about the extension (if available/given), including: | 1
|   |    - Name
|   |    - Logo
|   |    - Publisher
|   |    - Free/Paid
|   |    - Description
|   |    - Screenshots
|   |    - Release Date
|   |    - Approximate Size
|   | And actions (buttons/links), including:
|   |        - Install/Buy (depending on if the extension is free or paid)
|   |        - View in Microsoft Store
|   |        - Terms of Transaction
|   |        - Permissions Info 
| 3.1 | Dev Home has a built-in Extensions Library | 0 |
| 3.2 | The extensions library page shows all installed extensions (including extensions installed from the local file system) | 0 |
| 3.3 | The extensions library page allows users to disable extensions directly from Dev Home and directs users to the Settings > Apps > Installed Apps page to handle the uninstall of extensions/apps | 0 |
| 3.4 | The extensions library page allows users to check for available updates for their installed extensions directly from Dev Home | 0 |
| 3.5 | The extensions library page allows users to update their installed extensions directly from Dev Home | 0 |
| 3.6 | The extensions library page can sort installed extensions by: | 1 
|   |    - Publisher
|   |    - Size
|   |    - Last installed
|   |    - Last Updated
|   |    - Installation Mechanism (Extensions Marketplace, Microsoft Store, Intune, Locally)
| 3.7 | Dev Home recognizes when a user has uninstalled an extension via Microsoft Store or Settings and updates this in the Extensions Library view by no longer including the installed extension in the library list | 0 |
| 3.8 | Should an extension require an OS restart in order for changes to deploy, Dev Home communicates this need to the user within the Extensions Library page.| 0 |
| 4.1 | Organizations can manage (install, update, remove, block) select extensions in Dev Home (i.e., via Intune) | 0 |
| 4.2 | GPO can manage (install, update, remove, block) extensions on an individual or global basis in Dev Home (i.e., via Intune) | 0 |