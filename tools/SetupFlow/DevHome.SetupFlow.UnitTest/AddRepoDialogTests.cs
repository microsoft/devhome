using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
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
}
