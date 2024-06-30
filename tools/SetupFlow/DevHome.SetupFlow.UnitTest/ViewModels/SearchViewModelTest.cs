// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.SetupFlow.UnitTest.Helpers;
using DevHome.SetupFlow.ViewModels;
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
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(canceled: true)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.Canceled, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_CatalogNotConnected_ReturnsNotConnectedStatusAndNull()
    {
        WindowsPackageManager!.Setup(wpm => wpm.SearchAsync(It.IsAny<string>(), It.IsAny<uint>())).ThrowsAsync(new WindowsPackageManagerRecoveryException());
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.CatalogNotConnect, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_Exception_ReturnsExceptionStatusAndNull()
    {
        WindowsPackageManager!.Setup(wpm => wpm.SearchAsync(It.IsAny<string>(), It.IsAny<uint>())).ThrowsAsync(new InvalidOperationException());
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.ExceptionThrown, status);
        Assert.IsNull(packages);
    }

    [TestMethod]
    public void Search_NonEmptyText_ReturnsOkStatusAndNonNullResult()
    {
        WindowsPackageManager!.Setup(wpm => wpm.SearchAsync(It.IsAny<string>(), It.IsAny<uint>())).ReturnsAsync(new List<IWinGetPackage>()
        {
            // Mock a single result package
            PackageHelper.CreatePackage("mock").Object,
        });
        var searchViewModel = TestHost!.GetService<SearchViewModel>();

        var (status, packages) = searchViewModel.SearchAsync("mock", new CancellationToken(false)).GetAwaiter().GetResult();

        Assert.AreEqual(SearchViewModel.SearchResultStatus.Ok, status);
        Assert.AreEqual(1, packages.Count);
    }
}
