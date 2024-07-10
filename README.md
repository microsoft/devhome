![dev-home-readme-header](https://github.com/microsoft/devhome/blob/main/src/Assets/Preview/StoreDisplay-150.png)

# Welcome to the Dev Home repo!

Dev Home is a new experience from Microsoft aiming to give developers more power on Windows.

This repository contains the source code for:

* [Dev Home](https://aka.ms/devhome)
* Dev Home core widgets

Related repositories include:

* [Dev Home GitHub Extension](https://github.com/microsoft/devhomegithubextension)
* [Dev Home Azure Extension](https://github.com/microsoft/devhomeazureextension)

## Installing and running Dev Home

> **Note**: Dev Home requires Windows 11 21H2 (build 22000) or later.

If you are running Windows 11 23H2 (build 22621.2361) or later, you can install and run Dev Home just by finding it in the Start menu.

Otherwise, you can install [Dev Home from the Microsoft Store](https://aka.ms/devhome).
This allows you to always be on the latest version when we release new builds with automatic upgrades. Note that widgets may not work on older versions of Windows.

This is our preferred method.

### Other install methods

#### Via GitHub

For users who are unable to install Dev Home from the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/devhome/releases).

#### Via Windows Package Manager CLI (aka Winget)

[winget](https://github.com/microsoft/winget-cli) users can download and install the latest Dev Home release by installing the `Microsoft.DevHome` package:

```powershell
winget install --id Microsoft.DevHome -e
```

---

## Dev Home roadmap

The plan for Dev Home can be found in our [roadmap](docs/roadmap.md).

---

## Dev Home overview

Please take a few minutes to review the overview below before diving into the code:

### Dashboard

The Dev Home dashboard displays Windows widgets. These widgets are built using the Windows widget platform, which relies on Adaptive Cards.

### Machine configuration

The machine configuration tool utilizes the Dev Home GitHub Extension but isn't required to clone and install apps. The app installation tool is powered by WinGet.

#### Popular apps

The machine configuration tool lists popular apps when selecting applications to install. This is currently a hard-coded list of applications that have been popular with developers on Windows. High levels of installation and usage determined popularity. As this is a moment, we are not accepting submissions for this list. We're looking to improve the experience with [Suggested Apps](https://github.com/microsoft/devhome/issues/375) so the list can be optimized for developers.

---

## Documentation

Documentation for Dev Home can be found at https://aka.ms/devhomedocs.

---

## Contributing

We are excited to work alongside you, our amazing community, to build and enhance Dev Home!

***BEFORE you start to work on a feature/fix,*** please read & follow our [Contributor's Guide](CONTRIBUTING.md) to help avoid any wasted or duplicate effort.

## Communicating with the team

The easiest way to communicate with the team is via GitHub issues.

Please file new issues, feature requests, and suggestions but **DO search for similar open/closed preexisting issues before creating a new issue.**

If you would like to ask a question that you feel doesn't warrant an issue (yet), please reach out to us via Twitter:

* [Kayla Cinnamon](https://github.com/cinnamon-msft), Senior Product Manager: [@cinnamon_msft](https://twitter.com/cinnamon_msft)
* [Clint Rutkas](https://github.com/crutkas), Principal Product Manager: [@clintrutkas](https://twitter.com/clintrutkas) 
* [Leeza Mathew](https://github.com/mathewleeza), Engineering Lead: [@leezamathew](https://twitter.com/leezamathew)

## Developer Guidance

* You must be running Windows 11 21H2 (build >= 10.0.22000.0) to run Dev Home
* You must [enable Developer Mode in the Windows Settings app](https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development)

---

## Code of conduct

We welcome contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to and do actually grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not confuse or imply Microsoft sponsorship. Any use of third-party trademarks or logos is subject to those third parties' policies.

## Thanks to our contributors

<a href="https://github.com/microsoft/devhome/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=microsoft/devhome" />
</a>
