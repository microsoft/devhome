---
author: Sharla Soennichsen @shakersMSFT
created on: 2024-02-22
last updated: 2024-02-23
issue id: #2211
---

# Detect Configuration files in repositories

## 1. Overview

### 1.1 Establish the Problem

Setting up to build a repository is one of the more tedious tasks that a developer needs to do in their workflow. We have been building out WinGet Configuration files and the ability to specify the set up needs for a repository within a configuration file which will then configure applications, settings, and variables needed. We have been adding them to our repos to make it easier for set up but we don’t have an easy way to clone + find the configuration to complete the set up in one flow.  

### 1.2 Introduce the Solution

When a user clones a repository, we can check the repository for the existence of a WinGet configuration file, and allow the user to elect to apply that configuration file to easily fast track the set up process for any given repository. Configuration files will allow the repository owner to specify applications to install, dependencies, and even settings that can be configured all in one go. 

### 1.3 Rough-in Designs

*coming soon*

## 2. Goals & User Cans

### 2.1 Goals

1. Improve the discoverability of WinGet configuration files 
2. Make it easier to configure repos as part of the set up flow

### 2.2 Non-Goals

1. Run any configuration file other than Winget configuration files
2. Detect configuration files anywhere in a repo (must be in .configurations folder)
3. Allow the upload of a local configuration file as part of the e2e set up flow 

### 2.3 User Cans Summary Table

| No. | User Can | Pri |
| --- | -------- | --- |
| 1 | User can see if any repositories they cloned have a configuration file | 0 |
| 2 | User can see the associated repo for each configuration file | 0 |
| 3 | User can view the file location of each configuration file detected | 0 |
| 4 | User can choose to run any configuration file detected during set up | 0 |

## 3. User Stories

### 3.1 User story - Configuration files are automatically detected when cloning (user runs from Dev Home)

#### Job-to-be-done

User is setting up their development machine for the first time and has a few repositories they need to set up for development. 

#### User experience

1. User selects e2e machine configuration flow (or repo cloning quick action) 
2. User selects repos to clone 
3. Set up flow continues, user makes any other selections, and then starts the set up process 
4. During set up, repos are cloned 
5. Dev Home checks repos for configuration files and gathers the list of detected files 
6. On the summary page, user is notified of detected configuration files.  
7. User can view the list of all detected configuration files, and choose to view or run the file 
8. User selects to run a configuration file detected 
9. An admin command prompt is launched with the configuration command and file pre-set 
10. User can opt to run the configuration file and finish setting up their repo 

#### Golden paths (with images to guide)

#### Edge cases

### 3.2 User story - Configuration files are automatically detected when cloning (user runs manually)

#### Job-to-be-done

#### User experience
1. User selects e2e machine configuration flow (or repo cloning quick action) 
2. User selects repos to clone 
3. Set up flow continues, user makes any other selections, and then starts the set up process 
4. During set up, repos are cloned 
5. Dev Home checks repos for configuration files and gathers the list of detected files 
6. On the summary page, user is notified of detected configuration files.  
7. User can view the list of all detected configuration files, and choose to view or run the file 
8. User selects to view the file location of a configuration file
9. User's file explorer is opened to the location of the configuration file within their cloned repo 

#### Golden paths (with images to guide)
1. (post flow) User navigates to the configuration file flow, and selects a configuration file from a cloned repo to run

#### Edge cases

## 4. Requirements

### 4.1 Functional Requirements

#### Summary

As a user goes through the machine configuration flow, we want to optimize their experience where we can. When they are cloning repos, we can check for WinGet configuration files and show those to help developers set up and configure repositories faster. 

#### Detailed Experience Walkthrough

*coming soon*

#### Detailed Functional Requirements

| No. | Requirement | Pri |
| --- | ----------- | --- |
| 1   | When a repo is cloned, check for a WinGet configuration file (*.dsc.yaml or *.winget in the .configurations folder) | 0 |
| 2   | The total number of detected configuration files from a flow is shown on the summary page  | 0 |
| 3   | A user can see file location or choose to run a configuration file from the summary page  | 0 |
| 4   | Viewing a configuration file links to its file location  | 0 |
| 5   | Running a configuration file opens up an admin command prompt with the WinGet Configure command populated with the selected file | 0 |
| 6   | User can elect to run the configuration file from the command prompt | 0 |
| 7   | Each 'run file' will open a new command prompt | 0 |
