<!-- TODO: Hero image here -->
![Hero Image](assets/hero.png)

<h1 align="center">
    Dev Home
</h1>
<p align="center">
  <a href="https://apps.microsoft.com/detail/9P3JFPWWDZRC?launch=true&mode=mini">
    <img src="assets/storeBadge.png" width="200" /></a>
</p>

Dev Home is a new experience from Microsoft aiming to give developers more power on Windows.

https://learn.microsoft.com/en-us/windows/dev-home

- **Customizable widgets:** Use the dashboard to monitor workflows, track your dev projects, coding tasks, Azure DevOps queries, GitHub issues, pull requests, available SSH connections, and system CPU, GPU, Memory, and Network performance.
- **Machine Configuration:** Set up your development environment on a new device or onboard a new dev project.
- **Extensible:** Set up widgets that display developer-specific information. Create and share your own custom-built extensions.
- **Dev Drive:** Create a Dev Drive to store your project files and Git repositories.

## Dev Home Extensions

Here're some popular extensions:

- [Dev Home GitHub Extension](https://github.com/microsoft/devhomegithubextension)
- [Dev Home Azure Extension](https://github.com/microsoft/devhomeazureextension)

## Getting started with Dev Home

> [!NOTE]
> Dev Home requires Windows 11 21H2 (build 22000) or later.

If you are running Windows 11 23H2 (build 22621.2361) or later, you can install and run Dev Home just by finding it in the Start menu.

Otherwise, you can install [from the Microsoft Store](https://aka.ms/devhome).
This allows you to always be on the latest version when we release new builds with automatic upgrades. Note that widgets may not work on older versions of Windows.

The full documentation can be found in [Microsoft Learn](https://learn.microsoft.com/windows/apps/desktop/):

### Installing via GitHub

For users who are unable to install Dev Home from the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/devhome/releases).

### Installing via WinGet (Windows Package Manager CLI)

[WinGet](https://github.com/microsoft/winget-cli) users can download and install the latest release by installing the `Microsoft.DevHome` package:

```powershell
winget install --id Microsoft.DevHome -e
```

## 🛣️ Roadmap

For info on the WinUI release schedule and high level plans please see the [Dev Home Roadmap](docs/roadmap.md).

## Dev Home overview

Please take a few minutes to review the overview below before diving into the code:

### Dashboard

The Dev Home dashboard displays Windows widgets. These widgets are built using the Windows widget platform, which relies on Adaptive Cards.

### Machine configuration

The machine configuration tool utilizes the Dev Home GitHub Extension, but isn't required to clone and install apps. The app installation tool is powered by winget.

## Contributing to Dev Home

We are excited to work alongside you, our amazing community, to build and enhance Dev Home!

***BEFORE you start work on a feature/fix,*** please read & follow our [Contributor's Guide](CONTRIBUTING.md) to help avoid any wasted or duplicate effort.

## Contact

The easiest way to communicate with the team is via GitHub issues.

Please file new issues, feature requests, and suggestions but **DO search for similar open/closed preexisting issues before creating a new issue.**

If you would like to ask a question that you feel doesn't warrant an issue (yet), please reach out to us via Twitter:

* [Kayla Cinnamon](https://github.com/cinnamon-msft), Senior Product Manager: [@cinnamon_msft](https://twitter.com/cinnamon_msft)
* [Clint Rutkas](https://github.com/crutkas), Principal Product Manager: [@clintrutkas](https://twitter.com/clintrutkas) 
* [Leeza Mathew](https://github.com/mathewleeza), Engineering Lead: [@leezamathew](https://twitter.com/leezamathew)

## Developer guidance

* You must be running Windows 11 21H2 (build >= 10.0.22000.0) to run Dev Home
* You must [enable Developer Mode in the Windows Settings app](https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development)

---

## Code of conduct

We welcome contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos is subject to those third-parties' policies.

## Thanks to our contributors

<a href="https://github.com/microsoft/devhome/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=microsoft/devhome" />
</a>
