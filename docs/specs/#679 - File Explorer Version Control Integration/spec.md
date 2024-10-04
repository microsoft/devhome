---
author: Ethan Fang @ethan-fang-MS/ethanfang@microsoft.com
created on: 2024-02-26
last updated: 2024-02-26
issue id: #679
---

# File Explorer Version Control (i.e. Git) Integration

## 1. Overview

### 1.1 Establish the Problem

Developers deal in source code, but they also deal in files.  Whether you rank by minutes or by launches, File Explorer is one of the top applications that developers use.  We can make developers more productive and efficient by surfacing developer focused version control metadata in this surface and close the loop for interaction in File Explorer to lead back into the developer workflow.​ 

### 1.2 Introduce the Solution

There will always be scenarios where a developer has to move a file via File Explorer.  Being able to view version control information including properties like file staging status and the current branch directly within File Explorer will help make developers more powerful.   

Our goal is to be better together.  Top clients lack basic functionality like the ability to view files, instead they direct users to File Explorer. We need to close this loop so developers aren’t stranded when working with version controlled-projects in File Explorer and we can do this by providing extensibility points for version control protocols (e.g. Git, SVN, Perforce) within File Explorer (accessible via Dev Home – more on this later). 

### 1.3 Rough-in Designs

![File Explorer with Git Integration](./image1-File%20Explorer.png)

![Settings in Dev Home](./image2-Settings%20in%20Dev%20Home.png)

## 2. Goals & User Cans

### 2.1 Goals
1. File Explorer provides consistent, up-to-date status updates for all version-controlled-repositories the user has elected to be included in the integration enhancement 
2. Developers work faster and more efficiently as a result of this feature 
3. The version control integration into File Explorer furthers the aim to modernize the application 



### 2.2 Non-Goals
1. File Explorer does not aim to replace third-party solutions for performing version control actions (i.e. push, pull, etc.) nor does it aim to support other actions like changing or merging branches

### 2.3 User Cans Summary Table
| No. | User Cans | Priority |
|----|----|----|
| 0 | Users can manage which version control protocols integrate with File Explorer | 0 |
| 1 | Users can visually understand that a directory (or sub-directory) is a version control repository (or a directory within a repository) | 0 |
| 2 | Users can open any version-controlled-repository in the local version control client or IDE of their choice directly from File Explorer | 1 |
| 3 | Users can quickly access (or select) all of their repositories within File Explorer with minimal navigation & clicks needed | 1 |
| 4 | Users can understand the status of files at a glance (e.g. for Git: modified, staged for commit, untracked, committed, etc.) | 0 |
| 5 | Users can understand the current branch and origin at a glance | 0 |
| 6 | Users can quickly access settings to customize their own File Explorer version control integration experience | 0 |


## 3. Requirements

### 3.1 Functional Requirements

#### Summary

Integrating version control protocols into File Explorer allows developers to intuitively and visually understand key status updates to files, quickly identify repositories via file navigation, understand the nature of the current branch & origin of a given repository, and quickly open their client or hosting solution of choice. Syncing to remote repos, online repository hosts, and other status updates will require sign-in to the relevant client or host. Much of this experience should and is intended to be possible even if the user is working without an internet connection. 

#### Detailed Functional Requirements

| No. | Requirement | Pri |
|----|----|----|
| 0.0 | The File Explorer Version Control Integration is extensible (& managed) via Dev Home | P0 |
| 0.1 | Repositories are manually marked for inclusion in File Explorer version control integration enhancement (user needs to turn a repository into an enhanced repository – this is done via settings, see 6.4) | P0 |
| 0.11 | Version control protocol extensions can add tag repositories as “enhanced repositories” via Dev Home Extensions SDK API | P0 |
| 0.2 | Repositories can be automatically included in file explorer integration enhancement (this would turn all known repositories into enhanced repositories) | P2 |
| 0.3 | Each version control protocol will need to be a Dev Home extension in order to provide File Explorer integration | P0 |
| 0.4 | Installed version control protocols are selected by the user on a per-repository basis (see 6.5) | P0 |
| 0.5 | The Git Extension for this integration will be 1st party-developed and installed in Dev Home by default | P0 |
| 0.6 | 3rd parties will be able to implement support for additional version control protocols (i.e. SVN, Perforce) using the Dev Home Extensions SDK | P0 |
| 1.1 | Enhanced version-controlled repository folders can be visually distinguishable from normal folders via icons (these folder icons are extensible; asset is provided by the managing version control extension – defaults to the regular folder icon if not provided) | P2 |
| 2.1 | All enhanced repositories can be quickly accessed from one location (more on this in coming "Repository Management" work) | P2 |
| 3.1 | File Explorer provides an extensible version control “Version control status” column (i.e. for Git: unchanged, modified, staged for commit, untracked, committed) in the default view for enhanced repositories (version control extensions will populate the data for this column for each line item in an enhanced repository) | P0 |
| 3.2 | File Explorer provides an extensible “Last commit date” column in the default view for enhanced repositories (version control extensions will populate the data for this column for each line item in an enhanced repository) | P0 |
| 3.3 | File Explorer provides an extensible “Last commit message” column in the default view for enhanced repositories (version control extensions will populate the data for this column for each line item in an enhanced repository) | P0 |
| 3.5 | To summarize, the default (**checked column items**) column view for enhanced repositories will be: | P0 |
|     | - Name | |
|     | - Size | |
|     | - Version control status (`System.VersionControl.Status`, supplied by Dev Home) | |
|     | - Last commit date (`System.VersionControl.LastChangeDate`, supplied by Dev Home) | |
|     | - Last commit message (capped at 50 characters) (`System.VersionControl.LastChangeMessage`, supplied by Dev Home) | |
|     | **Unchecked**: | |
|     | - Date modified | |
|     | - Type | |
|     | - Date created | |
|     | - Authors | |
|     | - Tags | |
|     | - Title | |
|     | - Last commit author (`System.VersionControl.LastChangeAuthorName`, supplied by Dev Home) | |
|     | - Last commit author email (`System.VersionControl.LastChangeAuthorEmail`, supplied by Dev Home) | |
|     | - Last commit ID (`System.VersionControl.LastChangeID`, supplied by Dev Home) | |
| 3.6 | For all extensions-populated data columns in File Explorer, columns can have data even when a user’s device is offline (up to the extension, for Git, data will exist in columns when offline) – otherwise columns will have blank data | P0 |
| 4.1 | Useful repository status (provided by the extension) can be shown in the status bar using the `System.VersionControl.CurrentFolderStatus` property (for Git, this will be used to show the current head/branch & remote) | P1 |


#### Settings Functional Requirements
The File Explorer Version Control integration feature ships with Dev Home so related settings for this feature will be housed inside of Dev Home under Developer+/Advanced Settings. Specific functional requirements for related settings include: 
| No. | Requirement | Priority |
|-----|-------------|----------|
| 6.1 | Enable (Turn On or Off) File Explorer version control integration | 0 |
| 6.2 | Enable (Turn On or Off) version control column view for enhanced repositories | 0 |
| 6.3 | Add/remove specific repositories (paths) for inclusion in File Explorer version control integration enhancement | 0 |
| 6.4 | Assign version control extension (i.e. Git) to specific repositories in list of repositories included in file explorer version control integration enhancement (default to “Choose a version control protocol”) | 0 |
| 6.5 | Enable automatically add all detected version-controlled repositories to be included in file explorer integration enhancement (this would turn all known repositories into enhanced repositories) | 1 |
