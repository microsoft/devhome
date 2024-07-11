# Dev Home Contributor's Guide

Below is our guidance for how to report issues, propose new features, and submit contributions via pull requests (PRs).

## Open development workflow

The Dev Home team is VERY active in this GitHub Repo. In fact, we live in it all day long and carry out all our development in the open!

When the team finds issues, we file them in the repo. When we propose new ideas or think up new features, we file new feature requests. When we work on fixes or features, we create branches and work on those improvements. And when PRs are reviewed, we review in public - including all the good, the bad, and the ugly parts.

The point of doing all this work in public is to ensure that we are holding ourselves to a high degree of transparency, and so that the community sees that we apply the same processes and hold ourselves to the same quality bar as we do to community-submitted issues and PRs. We also want to make sure that we expose our team culture and "tribal knowledge" that is inherent in any closely-knit team, which often contains considerable value to those new to the project who are trying to figure out "why the heck does this thing look/work like this???"

### Repo bot

The team triages new issues several times a week. During triage, the team uses labels to categorize, manage, and drive the project workflow.

We employ a bot engine to help us automate common processes within our workflow.

We drive the bot by tagging issues with specific labels which cause the bot engine to close issues, merge branches, etc. This bot engine helps us keep the repo clean by automating the process of notifying appropriate parties if/when information/follow-up is needed, and closing stale issues/PRs after reminders have remained unanswered for several days.

Therefore, if you do file issues or create PRs, please keep an eye on your GitHub notifications. If you do not respond to requests for information, your issues/PRs may be closed automatically.

---

## Reporting security issues

**Please do not report security vulnerabilities through public GitHub issues.** Instead, please report them to the Microsoft Security Response Center (MSRC). See [SECURITY.md](./SECURITY.md) for more information.

## Before you start, file an issue

Please follow this simple rule to help us eliminate any unnecessary wasted effort & frustration, and ensure an efficient and effective use of everyone's time - yours, ours, and other community members':

> 👉 If you have a question, think you've discovered an issue, would like to propose a new feature, etc., then find/file an issue **BEFORE** starting work to fix/implement it.

### Search existing issues first

Before filing a new issue, search existing open and closed issues first: This project is moving fast! It is likely someone else has found the problem you're seeing, and someone may be working on or have already contributed a fix!

If no existing item describes your issue/feature, great - please file a new issue:

### File a new issue

* Don't know whether you're reporting an issue or requesting a feature? File an issue
* Have a question that you don't see answered in docs, videos, etc.? File an issue
* Want to know if we're planning on building a particular feature? File an issue
* Got a great idea for a new feature? File an issue/request/idea
* Don't understand how to do something? File an issue
* Found an existing issue that describes yours? Great - upvote and add additional commentary/info/repro-steps/etc.

When you hit "New Issue", select the type of issue closest to what you want to report/ask/request.

### Complete the template

**Complete the information requested in the issue template, providing as much information as possible**. The more information you provide, the more likely your issue/ask will be understood and implemented. Helpful information includes:

* What device you're running (inc. CPU type, memory, disk, etc.)
* What build of Windows your device is running

  👉 Tip: Run the following in PowerShell Core

  ```powershell
  C:\> $PSVersionTable.OS
  Microsoft Windows 10.0.18909
  ```

  ... or in Windows PowerShell

  ```powershell
  C:\> $PSVersionTable.BuildVersion

  Major  Minor  Build  Revision
  -----  -----  -----  --------
  10     0      18912  1001
  ```

  ... or Cmd:

  ```cmd
  C:\> ver

  Microsoft Windows [Version 10.0.18900.1001]
  ```

* What tools and apps you're using (e.g. VS 2022, VSCode, etc.)
* Don't assume we're experts in setting up YOUR environment. Teach us to help you!
* **We LOVE detailed repro steps!** What steps do we need to take to reproduce the issue? Assume we love to read repro steps. As much detail as you can stand is probably _barely_ enough detail for us!
* Prefer error message text where possible or screenshots of errors if text cannot be captured.
* **If you intend to implement the fix/feature yourself then say so!** If you do not indicate otherwise we will assume that the issue is our to solve, or may label the issue as `Help-Wanted`.

### DO NOT post "+1" comments

> ⚠ DO NOT post "+1", "me too", or similar comments - they just add noise to an issue.

If you don't have any additional info/context to add but would like to indicate that you're affected by the issue, upvote the original issue by clicking its [+😊] button and hitting 👍 (+1) icon. This way we can actually measure how impactful an issue is.

---

## Contributing fixes / features

If you're able & willing to help fix issues and/or implement features, we'd love your contribution!

The best place to start is the list of ["good first issue"](https://github.com/microsoft/devhome/issues?q=is%3Aopen+is%3Aissue+label%3A%22Help+Wanted%22++label%3A%22good+first+issue%22+)s. These are bugs or tasks that we on the team believe would be easier to implement for someone without any prior experience in the codebase. Once you're feeling more comfortable in the codebase, feel free to just use the ["Help Wanted"](https://github.com/microsoft/devhome/issues?q=is%3Aopen+is%3Aissue+label%3A%22Help+Wanted%22+) label, or just find an issue you're interested in and hop in!

Generally, we categorize issues in the following way, which is largely derived from our old internal work tracking system:
* ["Bugs"](https://github.com/microsoft/devhome/issues?q=is%3Aopen+is%3Aissue+label%3A%22Issue-Bug%22+) are parts of Dev Home that are not quite working the right way. There's code to already support some scenario, but it's not quite working right. Fixing these is generally a matter of debugging the broken functionality and fixing the wrong code.
* ["Tasks"](https://github.com/microsoft/devhome/issues?q=is%3Aopen+is%3Aissue+label%3A%22Issue-Task%22+) are usually new pieces of functionality that aren't yet implemented for Dev Home. These are usually smaller features, which we believe
  - could be a single, atomic PR
  - don't require much design consideration, or we've already written the spec for the larger feature they belong to.
* ["Features"](https://github.com/microsoft/devhome/issues?q=is%3Aopen+is%3Aissue+label%3A%22Issue-Feature%22+) are larger pieces of new functionality. These are usually things we believe would require a larger discussion of how they should be implemented, or they'll require some complicated new settings. They might just be features that are composed of many individual tasks. Oftentimes, with features, we like to have a spec written before development work is started, to make sure we're all on the same page (see below).

Bugs and tasks are the easiest to get started with, but don't feel afraid of features either!

Generally, we like to assign issues that generally belong to somebody's area of expertise to the team member who owns that area. This doesn't mean the community can't jump in -- they should reach out and have a chat with the assignee to see if it'd be okay to take. If an issue was assigned more than a month ago, there's a good chance it's fair game to try yourself.

### Contributing to Windows Customization

If you'd like to suggest a new feature/improvement to Windows Customization, **you must first file an issue with the provided `Windows Customization` [template](https://github.com/microsoft/devhome/issues/new?template=feature_request_windows_customization.yml)**. This will help us understand what you're looking for and why, and will help us ensure that the feature is something that we can support in the long run. For changes that rely on registry key behaviors that are undocumented, we will first have to review with internal stakeholders how to support the desired functionality and may not be able to support them in the long term. In these cases, we may have to modify your PR with a different approach after chatting with the internal teams.

We will not accept or review PRs that add new features to Windows Customization without an associated issue that follows the `Windows Customization` template.

For bug fixes, please still use the existing [bug template](https://github.com/microsoft/devhome/issues/new?template=Bug_Report.yml). If you are able to fix the bug, please indicate that in the issue and we'll be happy to review your PR.

### To spec or not to spec

Some issues/features may be quick and simple to describe and understand. For such scenarios, once a team member has agreed with your approach, skip ahead to the section headed "Fork, Branch, and Create your PR", below.

Small issues that do not require a spec will be labelled `Issue-Bug` or `Issue-Task`.

However, some issues/features will require careful thought & formal design before implementation. For these scenarios, we'll request that a spec is written and the associated issue will be labeled `Issue-Feature`.

Specs help collaborators discuss different approaches to solve a problem, describe how the feature will behave, how the feature will impact the user, what happens if something goes wrong, etc. Driving towards agreement in a spec, before any code is written, often results in simpler code, and less wasted effort in the long run.

Specs will be managed in a very similar manner as code contributions so please follow the "[Fork, branch and create your PR](CONTRIBUTING.md#fork-clone-branch-and-create-your-pr)" section below.

### Writing / Contributing to a spec

To write/contribute to a spec: fork, branch, and commit via PRs as you would with any code changes.

Specs are written in markdown, stored under the [`\docs\specs`](./docs/specs) folder and named `[issue id] - [spec description].md`.

👉 **It is important to follow the spec templates and complete the requested information**. The available spec templates will help ensure that specs contain the minimum information & decisions necessary to permit development to begin. In particular, specs require you to confirm that you've already discussed the issue/idea with the team in an issue and that you provide the issue ID for reference.

Team members will be happy to help review specs and guide them to completion.

### Help wanted

Once the team has approved an issue/spec, development can proceed. If no developers are immediately available, the spec can be parked ready for a developer to get started. Parked specs' issues will be labeled "Help Wanted". To find a list of development opportunities waiting for developer involvement, visit the Issues and filter on [the Help-Wanted label](https://github.com/microsoft/devhome/labels/Help%20Wanted).

---

## Development

### Fork, clone, branch and create your PR

Once you've discussed your proposed feature/fix/etc. with a team member and you've agreed on an approach or a spec has been written and approved, it's time to start development:

1. Fork the repo if you haven't already
2. Clone your fork locally
3. Create & push a feature branch
4. Create a [Draft Pull Request (PR)](https://github.blog/2019-02-14-introducing-draft-pull-requests/)
5. Work on your changes
6. Build and see if it works

## Building the code

1. Clone the repository
2. Configure your system
   * Please use the [configuration file](.configurations/configuration.dsc.yaml). This can be applied by either:
     * Dev Home's machine configuration tool
     * WinGet configuration. If you have WinGet version [v1.6.2631 or later](https://github.com/microsoft/winget-cli/releases), run `winget configure .configurations/configuration.dsc.yaml` in an elevated shell from the project root so relative paths resolve correctly
   * Alternatively, if you already are running the minimum OS version, have Visual Studio installed, and have developer mode enabled, you may configure your Visual Studio directly via the .vsconfig file. To do this:
     * Open the Visual Studio Installer, select “More” on your product card and then "Import configuration"
     * Specify the .vsconfig file at the root of the repo and select “Review Details”

## Running & debugging

In Visual Studio, you should be able to build and debug Dev Home by hitting <kbd>F5</kbd>. Make sure to select either the `x64` or the `x86` platform and set DevHome as the selected startup project.

Alternatively,

- Open the Developer Command Prompt for Visual Studio
- Run `Build` from Dev Home's root directory.  You can pass in a list of platforms/configurations
- The Dev Home MSIX will be in your repo under `AppxPackages\x64\debug`


### Rules

- **Follow the pattern of what you already see in the code.**
- [Coding style](./docs/style.md).
- Try to package new ideas/components into libraries that have nicely defined interfaces.
- Package new ideas into classes or refactor existing ideas into a class as you extend.
- When adding new classes/methods/changing existing code: add new unit tests or update the existing tests.

### Code review

When you'd like the team to take a look (even if the work is not yet fully-complete), mark the PR as 'Ready For Review' so that the team can review your work and provide comments, suggestions, and request changes. It may take several cycles but the end result will be solid, testable, conformant code that is safe for us to merge.

### Merge

Once your code has been reviewed and approved by the requisite number of team members, it will be merged into the main branch. Once merged, your PR will be automatically closed.

---

## Thank you

Thank you in advance for your contribution! Now, [what's next on the list](https://github.com/microsoft/devhome/labels/Help%20Wanted)? 😜
