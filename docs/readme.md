# Dev Documentation

## Fork, Clone, Branch and Create your PR

Once you've discussed your proposed feature/fix/etc. with a team member, and you've agreed on an approach or a spec has been written and approved, it's time to start development:

1. Fork the repo if you haven't already
1. Clone your fork locally
1. Create & push a feature branch <!--1. Create a [Draft Pull Request (PR)](https://github.blog/2019-02-14-introducing-draft-pull-requests/)-->
1. Work on your changes

## Rules

- **Follow the pattern of what you already see in the code.**
- [Coding style](style.md).
- Try to package new ideas/components into libraries that have nicely defined interfaces.
- Package new ideas into classes or refactor existing ideas into a class as you extend.
- When adding new classes/methods/changing existing code: add new unit tests or update the existing tests.
<!--
## GitHub Workflow

- Before starting to work on a fix/feature, make sure there is an open issue to track the work.
- Add the `In progress` label to the issue, if not already present also add a `Cost-Small/Medium/Large` estimate and make sure all appropriate labels are set.
- If you are a community contributor, you will not be able to add labels to the issue, in that case just add a comment saying that you started to work on the issue and try to give an estimate for the delivery date.
- If the work item has a medium/large cost, using the markdown task list, list each sub item and update the list with a check mark after completing each sub item.
- When opening a PR, follow the PR template.
- When you'd like the team to take a look, (even if the work is not yet fully-complete), mark the PR as 'Ready For Review' so that the team can review your work and provide comments, suggestions, and request changes. It may take several cycles, but the end result will be solid, testable, conformant code that is safe for us to merge.
- When the PR is approved, let the owner of the PR merge it. For community contributions the reviewer that approved the PR can also merge it.
- Use the `Squash and merge` option to merge a PR, if you don't want to squash it because there are logically different commits, use `Rebase and merge`.
- We don't close issues automatically when referenced in a PR, so after the PR is merged:
  - mark the issue(s), that the PR solved, with the `Resolution-Fix-Committed` label, remove the `In progress` label and if the issue is assigned to a project, move the item to the `Done` status.
  - don't close the issue if it's a bug in the current released version since users tend to not search for closed issues, we will close the resolved issues when a new version is released.
  - if it's not a code fix that effects the end user, the issue can be closed (for example a fix in the build or a code refactoring and so on).
-->
## Compiling Dev Home

### Compiling Source Code

There are two ways to compile locally.

- Open the Developer Command Prompt for Visual Studio
- Run `Build` from Dev Home's root directory.  You can pass in a list of platforms/configurations
- The Dev Home MSIX will be in your repo under `AppxPackages\x64\debug`

Alternatively

- Open `DevHome.sln` in Visual Studio, in the `Solutions Configuration` drop-down menu select `Release` or `Debug`, from the `Build` menu choose `Build Solution`.

## How to create new Tools

1. Create a new directory with your tool's name under `tools` with three subdirectories `src`, `test`, and `uitest`
1. Create a new `WinUI 3 Class Library` project in your `src` directory
1. Create the `Strings\en-us` directories under `src`.  Add `Resources.resw` and include the following code:
    ```xml
    <data name="NavigationPane.Content" xml:space="preserve">
      <value>[Name of your tool that will appear in navigation menu]</value>
    </data>
    ```
1. Add a project reference from `DevHome` to your project
1. Add a project reference from your project to `DevHome.Common` project under [common](\common)
1. Create your XAML view and viewmodel.  Your view class must inherit from `ToolPage` and implement requirements.  Specifications for the [Dev Home tools API](interface.md).
1. Update [NavConfig.json](\src\NavConfig.json) with your tool.  Specifications for the [NavConfig.json schema](navconfig.md).

Example:
```cs
public partial class SampleToolPage : ToolPage
{
    public override string ShortName => "SampleTool";
}
```

## Implementation details

### Dev Home framework

The Dev Home project contains the wrapping framework for the Dev Home application.
It's responsible for:
- Loading the individual Dev Home tools.
<!-- - Managing various credentials for use by tools. -->

### [`Interface`](interface.md)

- Definition of the interface used by Dev Home framework to manage the tools. All tools must implement this interface.
<!-- - Definition of the interface used by tools to interact with the Dev Home framework. -->

### [`DevHome.Common`](common.md)

The common lib, as the name suggests, contains code shared by multiple tools and the Dev Home framework