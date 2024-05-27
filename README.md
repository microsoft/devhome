<!-- TODO: Hero image here -->
![Hero Image](assets/hero.png)

<h1 align="center">
    Dev Home
</h1>
<p align="center">
  <a href="https://aka.ms/devhome">
    <img src="assets/storeBadge.png" width="200" /></a>
</p>

[Dev Home](https://learn.microsoft.com/en-us/windows/dev-home) is a new experience from Microsoft aiming to give developers more power on Windows.

- **Customizable widgets:** Use the dashboard to monitor workflows, track your dev projects, coding tasks, Azure DevOps queries, GitHub issues, pull requests, available SSH connections, and system CPU, GPU, Memory, and Network performance.
- **Environment for developers:** Set up your development environment on a new device or onboard a new dev project.
- **Extensibility:** Set up widgets that display developer-specific information. Create and share your own custom-built extensions.
- **Maximize productivity:** Create a Dev Drive to store your project files and Git repositories.

## ⚙️ Machine Configuration

The [machine configuration](https://learn.microsoft.com/windows/dev-home/setup#machine-configuration) tool utilizes the Dev Home GitHub Extension, but isn't required to clone and install apps. The app installation tool is powered by winget.

**Popular apps**

The machine configuration tool provides a list of popular apps when selecting applications to install. This is currently a hard-coded list of applications that have been popular with developers on Windows. Popularity was determined by high levels of installation and usage. As this is a moment in time, we are not accepting submissions for this list. We're looking to improve the experience with [Suggested Apps](https://github.com/microsoft/devhome/issues/375) so the list can be optimized for developers.

## 🧰 Dev Home Extensions

Here're some popular extensions:

- [Dev Home GitHub Extension](https://github.com/microsoft/devhomegithubextension)
- [Dev Home Azure Extension](https://github.com/microsoft/devhomeazureextension)

## 🚀 Getting started with Dev Home

Your Windows has to be Windows 11.

Windows|Availability
---|---
Windows 10 or earlier|❌ Unavailable
Windows 11 21H2 (OS build 22000)|✅ Available on Microsoft Store
Windows 11 22H1 (OS build 22621)|✅ Available on Microsoft Store
Windows 11 23H2 (OS build 22631) or higher|✅ Pre-installed

The full documentation can be found in [Microsoft Learn](https://learn.microsoft.com/windows/apps/desktop):

- [Set up Development Environment](https://learn.microsoft.com/windows/dev-home/setup)
- [Set up Development Utilities](https://learn.microsoft.com/windows/dev-home/utilities)
- [Install Dev Home extensions](https://learn.microsoft.com/windows/dev-home/extensions)
- [Customize Windows setup](https://learn.microsoft.com/windows/dev-home/windows-customization)

### Installing via GitHub

For users who are unable to install Dev Home from the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/devhome/releases).

### Installing via WinGet (Windows Package Manager CLI)

[WinGet](https://github.com/microsoft/winget-cli) users can download and install the latest release by installing the `Microsoft.DevHome` package:

```powershell
winget install --id Microsoft.DevHome -e
```

### Requirements to develop Dev Home

- Windows 11 21H2 (OS build 22000) or higher
- [Enable Developer Mode on Windows](https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development)
- Visual Studio 2022
- Windows SDK
- Windows App SDK
- .NET 8

## 🛣️ Roadmap

For info on the application release cadence we're planning please see the [Dev Home Roadmap](docs/roadmap.md).

## 📢 Contributing to Dev Home

We are excited to work alongside you, our amazing community, to build and enhance Dev Home!

***BEFORE you start work on a feature/fix,*** please read & follow our [Contributor's Guide](CONTRIBUTING.md) to help avoid any wasted or duplicate effort.

## 📇 Contact

The easiest way to communicate with the team is via GitHub issues.

Please file new issues, feature requests, and suggestions but **DO search for similar open/closed preexisting issues before creating a new issue.**

If you would like to ask a question that you feel doesn't warrant an issue (yet), please reach out to us via Twitter:

* [Kayla Cinnamon](https://github.com/cinnamon-msft), Senior Product Manager: [@cinnamon_msft](https://twitter.com/cinnamon_msft)
* [Clint Rutkas](https://github.com/crutkas), Principal Product Manager: [@clintrutkas](https://twitter.com/clintrutkas) 
* [Leeza Mathew](https://github.com/mathewleeza), Engineering Lead: [@leezamathew](https://twitter.com/leezamathew)

### Code of conduct

We welcome contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

### Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos is subject to those third-parties' policies.

### Thanks to our contributors

<a href="https://github.com/microsoft/devhome/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=microsoft/devhome" />
</a>
