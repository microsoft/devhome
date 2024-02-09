// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;

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

    [TestMethod]
    public void SwitchToUrlScreenTest()
    {
        var addRepoViewModel = new AddRepoViewModel(TestHost.GetService<ISetupFlowStringResource>(), new List<CloningInformation>(), TestHost, Guid.NewGuid(), string.Empty, null);
        addRepoViewModel.ChangeToUrlPage();
        Assert.AreEqual(Visibility.Visible, addRepoViewModel.ShowUrlPage);
        Assert.AreEqual(Visibility.Collapsed, addRepoViewModel.ShowAccountPage);
        Assert.AreEqual(Visibility.Collapsed, addRepoViewModel.ShowRepoPage);
        Assert.IsTrue(addRepoViewModel.IsUrlAccountButtonChecked);
        Assert.IsFalse(addRepoViewModel.IsAccountToggleButtonChecked);
        Assert.IsFalse(addRepoViewModel.ShouldShowLoginUi);
    }

    [TestMethod]
    public void SwitchToRepoScreenTest()
    {
        var addRepoViewModel = new AddRepoViewModel(TestHost.GetService<ISetupFlowStringResource>(), new List<CloningInformation>(), TestHost, Guid.NewGuid(), string.Empty, null);
        addRepoViewModel.ChangeToRepoPage(new ());
        Assert.AreEqual(Visibility.Collapsed, addRepoViewModel.ShowUrlPage);
        Assert.AreEqual(Visibility.Collapsed, addRepoViewModel.ShowAccountPage);
        Assert.AreEqual(Visibility.Visible, addRepoViewModel.ShowRepoPage);
        Assert.IsFalse(addRepoViewModel.ShouldShowLoginUi);
    }
}
