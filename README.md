# Welcome to the Dev Home repo

This repository contains the source code for:

* [Dev Home](https://aka.ms/devhome)
* Dev Home core widgets

Related repositories include:

* [Dev Home GitHub Extension](https://github.com/microsoft/devhomegithubextension)

## Installing and running Dev Home

> **Note**: Dev Home requires Windows 10 21H2 (build 22000) or later.

### Microsoft Store [Recommended]

Install [Dev Home from the Microsoft Store](https://aka.ms/devhome).
This allows you to always be on the latest version when we release new builds with automatic upgrades.

This is our preferred method.

### Other install methods

#### Via GitHub

For users who are unable to install Dev Home from the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/devhome/releases).

#### Via Windows Package Manager CLI (aka winget)

[winget](https://github.com/microsoft/winget-cli) users can download and install the latest Dev Home release by installing the `Microsoft.DevHome` package:

```powershell
winget install --id Microsoft.DevHome -e
```

---

## Dev Home roadmap

The plan for Dev Home will be posted shortly and will be updated as the project proceeds.

---

## Dev Home overview

Please take a few minutes to review the overview below before diving into the code:

### Dashboard

The Dev Home dashboard is built as a Windows widget renderer. These widgets are built using the Windows widget platform, which relies on Adaptive Cards.

### Machine configuration

The machine configuration tool utilizes the Dev Home GitHub extension, but isn't required to clone and install apps. The app installation tool is powered by winget.

#### Popular apps

Within the machine configuration tool is a popular apps list that appears when choosing applications to install. This is currently a hard-coded list of applications that have been popular with developers on Windows. Popularity was determined by high levels of installation and usage. As this is a moment in time, we are not accepting submissions for this list. We're looking to improve the experience with [Suggested Apps](https://github.com/microsoft/devhome/issues/375) so the list can be optimized for developers.

---

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Getting your machine setup to build
- clone the repository
- Configure your system, please use the [configuration file](.configurations/configuration.dsc.yaml).  This can be applied either:
   1. Using Dev home.
   2. WinGet configuration. If you have the experimental feature enabled, run `winget configure .configurations/configuration.dsc.yaml` from the project root so relative paths resolve correctly.


## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
