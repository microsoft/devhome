// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.UnitTest;

[TestClass]
public class LoadingScreenTests : BaseSetupFlowTest
{
    [TestMethod]
    public void HideRetryBannerTest()
    {
        var loadingViewModel = TestHost!.GetService<LoadingViewModel>();

        loadingViewModel.HideMaxRetryBanner();

        Assert.IsFalse(loadingViewModel.ShowOutOfRetriesBanner);
    }
}
