// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.UnitTest;

[TestClass]
public class LoadingScreenTests : BaseSetupFlowTest
{
    [TestMethod]
    public void HideRetryBannerTest()
    {
        var loadingViewModel = new LoadingViewModel(StringResource.Object, null, TestHost);

        loadingViewModel.HideMaxRetryBanner();

        Assert.IsFalse(loadingViewModel.ShowOutOfRetriesBanner);
    }
}
