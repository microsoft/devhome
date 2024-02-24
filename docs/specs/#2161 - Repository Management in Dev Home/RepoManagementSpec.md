---
author: Sharla Soennichsen @shakersMSFT
created on: 2024-02-22
last updated: 2024-02-23
issue id: #2161
---

# Repository management in Dev Home

## 1. Overview

### 1.1 Establish the Problem

Today you can clone repositories onto your machine through Dev Home, but afterwards, there is no way to check which repos are cloned already, recently updated, or do any sort of management or launching of those repos.

### 1.2 Introduce the Solution

The proposed experience is a new page to manage your cloned repositories on your machine through Dev Home. Through this view you can see the list of repositories you have cloned through Dev Home, point Dev Home at repos you may have cloned outside of Dev Home, and any new repos you clone through Dev Home are automatically added to this list. You will be able to see relevant information, take actions on the repos like launching or creating a new widget based on that repo for your Dev Home Dashboard.

### 1.3 Rough-in Designs

*coming soon*

## 2. Goals & User Cans

### 2.1 Goals

1. Provide users with a way to see and manage all repos they have cloned on their machine 

### 2.2 Non-Goals

1. We do not want to become another version control or repository hosting service 
2. We do not want to become a file editing/management system (if a user wants to view  or edit the contents of a file or folder in a repo, they need to choose to launch their repo management tool, IDE, etc.) 
3. We do not want to do ‘repo actions’ (e.g. push/pull)

### 2.3 User Cans Summary Table

| No. | User Can | Pri |
| --- | -------- | --- |
| 1 | User can see all repos they have cloned via Dev Home  | 0 |
| 2 | User can see the repo name, current clone path, and origin  | 0 |
| 3 | User can create a Dev Home widget for a selected repo   | 0 |
| 4 | User can ‘open with’ to launch their IDE with the repo   | 0 |
| 5 | User can ‘open with’ file explorer to view the files   | 0 |
| 6 | User can check against source to verify it is up to date   | 0 |
| 7 | User can opt to clone a new repo, which takes the user to the ‘quick step’ of cloning a repo in machine configuration  | 0 |
| 8 | User can point to file location of existing (cloned) repos on their machine to add to their list of known cloned repos   | 1 |
| 9 | User can see and run configuration files associated with a given repo   | 0 |

## 3. User Stories



### 3.1 User story - Cloning repos on a new machine

#### Job-to-be-done

A user is setting up their machine for a new project. They need to clone multiple repos and want to keep track of what they have done already for this new project and get started with development as soon as possible

#### User experience

1. User goes through the e2e machine configuration flow and clones repos and installs applications
2. Set up completes and the user can launch the repo management page from the summary page
2. User goes to the repo management page and can see the repos that they just cloned.
3. User selects a repo to launch with their IDE
5. Default IDE launches with the repo open

## 4. Requirements

### 4.1 Functional Requirements

#### Summary

The repo managment page will allow users to maintain a list of all cloned repos on their machine, launch them, create new widgets, complete their set up process, and utilize their repos within Dev Home experiences. 

#### Detailed Experience Walkthrough

*coming soon*

#### Detailed Functional Requirements

| No. | Requirement | Pri |
| --- | ----------- | --- |
| 1 | List out all repos the user has cloned via Dev Home   | 0 |
| 2 | When a user clones new repos via the set up flow, add the repos to the list   | 0 |
| 3 | Repo metadata includes repo name, clone path, and origin  | 0 |
| 4 | A repo can be launched with the IDE of choice  | 0 |
| 5 | A repo can be opened with file explorer to view the file location | 0 |
| 6 | There is an option to clone a new repo, which takes the user to the clone repo flow in machine configuration  | 0 |
| 7 | A Dev Home widget can be created from a selected repo  | 1 |
| 8 | If there is a configuration file associated with a repo, it can be viewed or run | 1 |
| 9 | The user can browse/point Dev Home to a file location to track existing cloned repos  | 1 |
