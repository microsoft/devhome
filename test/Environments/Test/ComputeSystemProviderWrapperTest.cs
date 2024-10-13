// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Environments.Models;
using DevHome.Test.Environments.Helpers;
using DevHome.Test.Environments.Models;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Test.Environments.Test;

[TestClass]
public class ComputeSystemProviderWrapperTest
{
    [TestMethod]
    public void ComputeSystemProviderWrapper_Returns_ComputeSystems()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId, false);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), new CancellationToken(false)).Result;
        var computeSystems = computeSystemsResult.ComputeSystems.ToList();
        var numberOfComputeSystems = 1;

        // Verify that the result is successful and that the number of compute systems returned is correct
        Assert.AreEqual(ProviderOperationStatus.Success, computeSystemsResult.Result.Status);
        Assert.AreEqual(numberOfComputeSystems, computeSystems.Count);
    }

    [TestMethod]
    public void ComputeSystemProviderWrapper_Does_Not_Allow_Exceptions_To_Escape_When_Retrieving_ComputeSystems()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId, true);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), new CancellationToken(false)).Result;

        // Verify that the result is a failure and that the exception was caught within the wrapper.
        Assert.AreEqual(ProviderOperationStatus.Failure, computeSystemsResult.Result.Status);
    }

    [TestMethod]
    public void ComputeSystemProviderWrapper_Returns_DisplayName()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId, false);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);

        Assert.AreEqual(TestHelpers.ComputeSystemProviderDisplayName, computeSystemProviderWrapper.DisplayName);
    }

    [TestMethod]
    public void ComputeSystemProviderWrapper_Returns_Id()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId, false);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);

        Assert.AreEqual(TestHelpers.ComputeSystemProviderId, computeSystemProviderWrapper.Id);
    }

    [TestMethod]
    public void ComputeSystemProviderWrapperReturnsSupportedOperations()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId, false);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);

        Assert.AreEqual(ComputeSystemProviderOperations.CreateComputeSystem, computeSystemProviderWrapper.SupportedOperations);
    }
}
