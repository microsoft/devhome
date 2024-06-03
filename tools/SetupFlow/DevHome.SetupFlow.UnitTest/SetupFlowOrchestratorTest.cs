// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Moq;
using Moq.Protected;

namespace DevHome.SetupFlow.UnitTest;

/// <summary>
/// Tests for the <see cref="SetupFlowOrchestrator"/>.
/// </summary>
[TestClass]
public class SetupFlowOrchestratorTest : BaseSetupFlowTest
{
    // Some of the methods we need to mock are protected.
    // For protected members, Moq takes a string with their name.
    // Define all the strings we need here.
    private static class ProtectedMembers
    {
        internal static readonly string OnFirstNavigateToAsync = nameof(OnFirstNavigateToAsync);
        internal static readonly string OnFirstNavigateFromAsync = nameof(OnFirstNavigateFromAsync);
    }

    /// <summary>
    /// Creates a <see cref="SetupFlowOrchestrator"/> and a list of
    /// <paramref name="count"/> mock pages that use it.
    /// </summary>
    private (SetupFlowOrchestrator, List<Mock<SetupPageViewModelBase>>) CreateMockPages(int count)
    {
        var orchestrator = new SetupFlowOrchestrator(null);

        var stringResource = TestHost.GetService<ISetupFlowStringResource>();
        var pageMocks = Enumerable.Range(0, count).Select(_ => new Mock<SetupPageViewModelBase>(stringResource, orchestrator)).ToList();

        return (orchestrator, pageMocks);
    }

    /// <summary>
    /// Tests that the navigation hook methods are called when appropriate.
    /// </summary>
    [TestMethod]
    public async Task Orchestrator_NavigationnHooks_Called()
    {
        // Create 3 mock pages and configure for checking the calls to the navigation hooks.
        var (orchestrator, pageMocks) = CreateMockPages(3);
        foreach (var pageMock in pageMocks)
        {
            pageMock.Protected().Setup(ProtectedMembers.OnFirstNavigateToAsync);
            pageMock.Protected().Setup(ProtectedMembers.OnFirstNavigateFromAsync);
        }

        // This list of expected number of calls for each member of each mock
        // will be used to verify everything at once.
        // Initially, none of the members should have been called.
        var expectedNumberOfCalls = Enumerable.Range(0, 3)
            .Select(_ => new Dictionary<string, Times>
            {
                { ProtectedMembers.OnFirstNavigateToAsync, Times.Never() },
                { ProtectedMembers.OnFirstNavigateFromAsync, Times.Never() },
            })
            .ToList();
        VerifyProtectedMembersCalled(pageMocks, expectedNumberOfCalls);

        // When we set the pages for the flow, we change to the first page on on the list,
        // so we should call its hook for navigating into it.
        var flowPages = pageMocks.Select(mock => mock.Object).ToList();
        orchestrator.FlowPages = flowPages;

        expectedNumberOfCalls[0][ProtectedMembers.OnFirstNavigateToAsync] = Times.Once();
        VerifyProtectedMembersCalled(pageMocks, expectedNumberOfCalls);

        // When we advance, we move from the first page to the second one,
        // so these methods should be called.
        await orchestrator.GoToNextPage();
        expectedNumberOfCalls[0][ProtectedMembers.OnFirstNavigateFromAsync] = Times.Once();
        expectedNumberOfCalls[1][ProtectedMembers.OnFirstNavigateToAsync] = Times.Once();
        VerifyProtectedMembersCalled(pageMocks, expectedNumberOfCalls);

        // Same thing when advancing again for the second and third pages
        await orchestrator.GoToNextPage();
        expectedNumberOfCalls[1][ProtectedMembers.OnFirstNavigateFromAsync] = Times.Once();
        expectedNumberOfCalls[2][ProtectedMembers.OnFirstNavigateToAsync] = Times.Once();
        VerifyProtectedMembersCalled(pageMocks, expectedNumberOfCalls);

        // If we go back, we should not trigger the hooks again
        await orchestrator.GoToPreviousPage();
        VerifyProtectedMembersCalled(pageMocks, expectedNumberOfCalls);

        await orchestrator.GoToPreviousPage();
        VerifyProtectedMembersCalled(pageMocks, expectedNumberOfCalls);
    }

    /// <summary>
    /// Verifies that the protected members in mocks are called the expected number of times.
    /// </summary>
    /// <param name="mocks">The mock objects to verify.</param>
    /// <param name="expectedCalls">
    /// A list of dictionaries corresponding to the elements of <paramref name="mocks"/>.
    /// Each entry contains a mapping from member name to the expected number of calls.
    /// </param>
    private void VerifyProtectedMembersCalled<T>(IEnumerable<Mock<T>> mocks, IEnumerable<IDictionary<string, Times>> expectedCalls)
        where T : class
    {
        // Sanity check: There must be a one-to-one correspondence between the two lists
        Assert.AreEqual(mocks.Count(), expectedCalls.Count());

        foreach (var (mock, expectedCallsForMock) in mocks.Zip(expectedCalls))
        {
            foreach (var (member, times) in expectedCallsForMock)
            {
                mock.Protected().Verify(member, times);
            }
        }
    }

    /// <summary>
    /// Tests that the flags indicating the position of each page are set correctly.
    /// </summary>
    /// <remarks>
    /// These flags are used for the stepper control at the top of the page.
    /// </remarks>
    [TestMethod]
    public async Task Orchestrator_PagePositionFlags()
    {
        // Create 5 mock pages and navigate to the end verifying the positioning flags
        const int Count = 5;
        var (orchestrator, pageMocks) = CreateMockPages(Count);
        var flowPages = pageMocks.Select(mock => mock.Object).ToList();
        orchestrator.FlowPages = flowPages;

        // Set some pages as Step pages and verify that the right one is marked as the last one.
        flowPages[0].IsStepPage = true;
        flowPages[1].IsStepPage = true;
        flowPages[2].IsStepPage = false;
        flowPages[3].IsStepPage = true;
        flowPages[4].IsStepPage = false;

        Assert.IsFalse(flowPages[0].IsLastStepPage);
        Assert.IsFalse(flowPages[1].IsLastStepPage);
        Assert.IsFalse(flowPages[2].IsLastStepPage);
        Assert.IsTrue(flowPages[3].IsLastStepPage);
        Assert.IsFalse(flowPages[4].IsLastStepPage);

        // Navigate all the way to the end verifying that the pages are correctly marked as past or upcoming
        for (var i = 0; i < Count; i++)
        {
            // Navigate to the i-th page; no need to navigate to the first one
            if (i != 0)
            {
                await orchestrator.GoToNextPage();
            }

            for (var j = 0; j < i; j++)
            {
                Assert.IsTrue(flowPages[j].IsPastPage);
                Assert.IsFalse(flowPages[j].IsCurrentPage);
                Assert.IsFalse(flowPages[j].IsUpcomingPage);
            }

            Assert.AreSame(orchestrator.CurrentPageViewModel, flowPages[i]);
            Assert.IsFalse(flowPages[i].IsPastPage);
            Assert.IsTrue(flowPages[i].IsCurrentPage);
            Assert.IsFalse(flowPages[i].IsUpcomingPage);

            for (var j = i + 1; j < Count; j++)
            {
                Assert.IsFalse(flowPages[j].IsPastPage);
                Assert.IsFalse(flowPages[j].IsCurrentPage);
                Assert.IsTrue(flowPages[j].IsUpcomingPage);
            }
        }
    }
}
