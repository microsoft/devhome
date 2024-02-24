---
author: Sharla Soennichsen @shakersMSFT
created on: 2024-02-21
last updated: 2024-02-23
issue id: #2160
---

# Dev Home machine configuration generates a configuration file

## 1. Overview

### 1.1 Establish the Problem

Today a user can specify details for setting up their machine in the end to end machine configuration flow, but they don't have a way to easily repeat that set up. Winget configuration files make it easy and repeatable to configure a machine like the end to end flow, but there isn't a way to generate a configuration file easily today.

### 1.2 Introduce the Solution

In the proposed solution, the developer doesn’t need to modify how they are using the end to end flow today, they can make the same selections and on the last step they get to choose whether they want to save those steps as a configuration file to share with teammates or use to set up another machine for development.

### 1.3 Rough-in Designs

*Coming soon*

## 2. Goals & User Cans

### 2.1 Goals

1. Make it easy to create a configuration file through Dev Home 
2. Increase the number of Winget configuration files available to make it easy to configure and share


### 2.2 Non-Goals

1. Build out a full new UI for generating, editing, customizing a configuration file (i.e. we are not building a form/new wizard at this time) 

### 2.3 User Cans Summary Table

| No. | User Can | Pri |
| --- | -------- | --- |
| 1 | User can generate a winget configuration file as part of any setup option (except running a config file) | 0 |
| 2 | User can choose to set up & generate a file  | 0 |
| 3 | User can choose to just generate a file | 0 |
| 4 | User can choose to just set up and not generate a file  | 0 |
| 5 | User can select a local save location for their configuration file  | 0 |
| 6 | User can specify the file name  | 0 |
| 7 | User can specify the file extension as .dsc.yaml or .winget(default)  | 0 |

## 3. User Stories

### 3.1 User story - Setting up a new machine

#### Job-to-be-done

A user is setting up their machine for the first time, and wants to make it easy for themselves to get set up in the future.  

#### User experience

1. User navigates to e2e machine configuration flow 
2. User specifies repos to clone to their machine 
3. User adds a dev drive 
4. User adds applications to install 
5. User reviews their selections and elects to ‘save as’ a configuration file 
6. User specifies a file name, file extension, and save location for their configuration file 
7. A configuration file is generated and saved 
8. User then clicks ‘set up’ 
9. Set up runs and the machine is configured

#### Golden paths (with images to guide)

#### Edge cases
1. User specifies some private repos to clone. The private repos will be cloned to their local machine during set up but will be commented out in their configuration file due to authentication restrictions. 

### 3.2 User story - Create a file on an existing machine

#### Job-to-be-done

A user has an existing dev machine that they use and they want to use dev home to generate a configuration file based on their machine.  

#### User experience

1. User navigates to e2e machine configuration flow 
2. User specifies repos to clone
3. User adds a dev drive 
4. User adds applications to install (some may already be installed)
5. User reviews their selections and elects to ‘save as’ a configuration file 
6. User specifies a file name, file extension, and save location for their configuration file 
7. A configuration file is generated and saved 
8. User then clicks ‘set up’ 
9. Set up runs and the machine is configured

#### Golden paths (with images to guide)

#### Edge cases
1. User specifies some private repos to clone. The private repos will be cloned to their local machine during set up but will be commented out in their configuration file due to authentication restrictions.

## 4. Requirements

### 4.1 Functional Requirements

#### Summary

On the review page of the end to end flow in machine configuration, the user can opt to generate a configuration file, which will then generate a configuration file that contains the set of items that the user specified they wanted included in their set up such as repos, dev drive, and applications. They can also specify the name, save location, and file type for their configuration file for use on another machine or for sharing with others. 

#### Detailed Experience Walkthrough

*Coming soon*

#### Detailed Functional Requirements

| No. | Requirement | Pri |
| --- | ----------- | --- |
| 1   |A configuration file can be generated from any flow except the ‘run a configuration file’ flow | 0 |
| 2   |A configuration file is generated based on the selections a user makes during the set up flow (repos, dev drive, applications) |   0  |
| 3   |On the review page, there is a ‘save as’ configuration file button | 0 |
| 4   |A configuration file generated from Dev Home indicates it was generated through Dev Home | 0 |
| 5   |Default file name is configuration.winget | 0 |
| 6   |User can select to generate a dsc.yaml and/or a .winget file (default .winget) | 0 |
| 7   |If the user selected private repos to clone, notify them that they will be commented out due to the authentication requirement and auth not being supported in DSC yet. | 0 |
| 8   |The summary page provides a link to the file location of the configuration file generated. | 0 |
| 9   |Configuration file generation follows best practices for creation | 0 |
| 10   |The configuration file generated always uses the latest schema https://aka.ms/configuration-dsc-schema/ | 0 |
| 11   |The default DSC module/resource for cloning repos is GitDSC/GitClone | 0 |
| 12   |The default DSC module/resource for creating dev drives is StorageDSC/Disk | 0 |
| 13   |The default DSC module/resource for installing applications is Microsoft.WinGet.DSC/WinGetPackage | 0 |
| 14   |If there is a known dependency for a module/resource, ensure the dependency is included and tagged as such in the configuration file | 0 |
| 15   |Installed applications added to a configuration file can include app settings | 3 |
