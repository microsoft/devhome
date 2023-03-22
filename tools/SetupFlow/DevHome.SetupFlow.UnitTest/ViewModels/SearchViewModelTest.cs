// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.AppManagement.ViewModels;
using Moq;

namespace DevHome.SetupFlow.UnitTest.ViewModels;

[TestClass]
public class SearchViewModelTest : BaseSetupFlowTest
{
    [TestMethod]
    [DataRow("", DisplayName = $"{nameof(Search_NullOrEmptyText_ReturnsEmptyStatusAndNull)}_Empty")]
    [DataRow(" ", DisplayName = $"{nameof(Search_NullOrEmptyText_ReturnsEmptyStatusAndNull)}_Space")]
    [DataRow(null, DisplayName = $"{nameof(Search_NullOrEmptyText_ReturnsEmptyStatusAndNull)}_Null")]
    public void Search_NullOrEmptyText_ReturnsEmptyStatusAndNull(string text)
    {
        var searchViewModel = TestHost!.GetService<SearchViewModel>();
        var (status, packages) = searchViewModel.SearchAsync(text, new CancellationToken(canceled: false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.EmptySearchQuery, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_CancelledToken_ReturnsCancelledStatusAndNull()
    {
        var allcatalogs = new Mock<IWinGetCatalog>();
        allcatalogs.Setup(c => c.IsConnected).Returns(true);
        var searchViewModel = TestHost!.GetService<SearchViewModel>();
        WindowsPackageManager!.Setup(wpm => wpm.AllCatalogs).Returns(allcatalogs.Object);

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(canceled: true)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.Canceled, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_CatalogNotConnected_ReturnsNotConnectedStatusAndNull()
    {
        var allcatalogs = new Mock<IWinGetCatalog>();
        allcatalogs.Setup(c => c.IsConnected).Returns(false);
        WindowsPackageManager!.Setup(wpm => wpm.AllCatalogs).Returns(allcatalogs.Object);
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.CatalogNotConnect, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_Exception_ReturnsExceptionStatusAndNull()
    {
        var allcatalogs = new Mock<IWinGetCatalog>();
        allcatalogs.Setup(c => c.IsConnected).Returns(true);
        allcatalogs.Setup(c => c.SearchAsync(It.IsAny<string>(), It.IsAny<uint>())).ThrowsAsync(new InvalidOperationException());
        WindowsPackageManager!.Setup(wpm => wpm.AllCatalogs).Returns(allcatalogs.Object);
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.ExceptionThrown, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_NonEmptyText_ReturnsOkStatusAndNonNullResult()
    {
        var allcatalogs = new Mock<IWinGetCatalog>();
        allcatalogs.Setup(c => c.IsConnected).Returns(true);
        allcatalogs.Setup(c => c.SearchAsync(It.IsAny<string>(), It.IsAny<uint>())).ReturnsAsync(new List<IWinGetPackage>()
        {
            // Mock a single result package
            new Mock<IWinGetPackage>().Object,
        });
        WindowsPackageManager!.Setup(wpm => wpm.AllCatalogs).Returns(allcatalogs.Object);
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.Ok, status);
        Assert.AreEqual(1, packages.Count);
    }
}
