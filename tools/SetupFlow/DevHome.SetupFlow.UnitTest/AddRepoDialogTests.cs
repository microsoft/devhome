// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.UnitTest;

[TestClass]
public class AddRepoDialogTests : BaseSetupFlowTest
{
    [TestMethod]
    [Ignore("AddRepoViewModel's constructor accepts a non-service known item.")]
    public void HideRetryBannerTest()
    {
        var addRepoViewModel = TestHost!.GetService<AddRepoViewModel>();

        addRepoViewModel.RepoProviderSelectedCommand.Execute(null);
        Assert.IsFalse(addRepoViewModel.ShouldEnablePrimaryButton);

        addRepoViewModel.RepoProviderSelectedCommand.Execute("ThisIsATest");
        Assert.IsTrue(addRepoViewModel.ShouldEnablePrimaryButton);
    }

    [UITestMethod]
    [Ignore("Making a new frame throws a COM exception.  Running this as UITestMethod does not help")]
    public void SwitchToUrlScreenTest()
    {
        var orchestrator = TestHost.GetService<SetupFlowOrchestrator>();
        var stringResource = TestHost.GetService<ISetupFlowStringResource>();
        var devDriveManager = TestHost.GetService<IDevDriveManager>();
        var addRepoViewModel = new AddRepoViewModel(orchestrator, stringResource, new List<CloningInformation>(), TestHost, Guid.NewGuid(), null, devDriveManager);
        addRepoViewModel.ChangeToUrlPage();
        Assert.AreEqual(true, addRepoViewModel.ShowUrlPage);
        Assert.AreEqual(false, addRepoViewModel.ShowAccountPage);
        Assert.AreEqual(false, addRepoViewModel.ShowRepoPage);
        Assert.IsTrue(addRepoViewModel.IsUrlAccountButtonChecked);
        Assert.IsFalse(addRepoViewModel.IsAccountToggleButtonChecked);
        Assert.IsFalse(addRepoViewModel.ShouldShowLoginUi);
    }

    [TestMethod]
    [Ignore("IextensionService uses Application.Current and tests break when Application.Current is used.  Ignore until fixed.")]
    public async Task SwitchToAccountScreenTest()
    {
        var orchestrator = TestHost.GetService<SetupFlowOrchestrator>();
        var stringResource = TestHost.GetService<ISetupFlowStringResource>();
        var devDriveManager = TestHost.GetService<IDevDriveManager>();
        var addRepoViewModel = new AddRepoViewModel(orchestrator, stringResource, new List<CloningInformation>(), TestHost, Guid.NewGuid(), null, devDriveManager);
        await addRepoViewModel.ChangeToAccountPageAsync();
        Assert.AreEqual(false, addRepoViewModel.ShowUrlPage);
        Assert.AreEqual(true, addRepoViewModel.ShowAccountPage);
        Assert.AreEqual(false, addRepoViewModel.ShowRepoPage);
        Assert.IsFalse(addRepoViewModel.ShouldShowLoginUi);
    }
}
