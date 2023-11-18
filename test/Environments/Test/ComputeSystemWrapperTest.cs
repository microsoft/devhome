// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Environments.Models;
using DevHome.Test.Environments.Helpers;
using DevHome.Test.Environments.Models;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Test.Environments.Test;
[TestClass]
public class ComputeSystemWrapperTest
{
    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Start |
                ComputeSystemOperations.ShutDown |
                ComputeSystemOperations.Terminate |
                ComputeSystemOperations.Delete |
                ComputeSystemOperations.Save |
                ComputeSystemOperations.Pause |
                ComputeSystemOperations.Resume |
                ComputeSystemOperations.Restart |
                ComputeSystemOperations.CreateSnapshot |
                ComputeSystemOperations.RevertSnapshot |
                ComputeSystemOperations.DeleteSnapshot |
                ComputeSystemOperations.ModifyProperties |
                ComputeSystemOperations.ApplyConfiguration;

    [TestMethod]
    public void ComputeSystemWrapper_Returns_Id()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), string.Empty).Result;
        var computeSystem = computeSystemsResult.ComputeSystems.ToList().First();

        Assert.AreEqual(TestHelpers.ComputeSystemId, computeSystem.Id);
    }

    [TestMethod]
    public void ComputeSystemWrapper_Returns_Name()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), string.Empty).Result;
        var computeSystem = computeSystemsResult.ComputeSystems.ToList().First();

        Assert.AreEqual(TestHelpers.ComputeSystemName, computeSystem.Name);
    }

    [TestMethod]
    public void ComputeSystemWrapper_Returns_AlternativeDisplayName()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), string.Empty).Result;
        var computeSystem = computeSystemsResult.ComputeSystems.ToList().First();

        Assert.AreEqual(TestHelpers.ComputeSystemAlternativeDisplayName, computeSystem.AlternativeDisplayName);
    }

    [TestMethod]
    public void ComputeSystemWrapper_Returns_AssociatedDeveloperId()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), string.Empty).Result;
        var computeSystem = computeSystemsResult.ComputeSystems.ToList().First();

        Assert.AreEqual(new TestDeveloperId().LoginId, computeSystem.AssociatedDeveloperId.LoginId);
    }

    [TestMethod]
    public void ComputeSystemWrapper_Returns_AssociatedProviderId()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), string.Empty).Result;
        var computeSystem = computeSystemsResult.ComputeSystems.ToList().First();

        Assert.AreEqual(TestHelpers.ComputeSystemProviderId, computeSystem.AssociatedProviderId);
    }

    [TestMethod]
    public void ComputeSystemWrapper_Returns_SupportedOperations()
    {
        var providerImpl = new TestComputeSystemProviderImpl(TestHelpers.ComputeSystemProviderDisplayName, TestHelpers.ComputeSystemProviderId);
        var computeSystemProviderWrapper = new ComputeSystemProvider(providerImpl);
        var computeSystemsResult = computeSystemProviderWrapper.GetComputeSystemsAsync(new TestDeveloperId(), string.Empty).Result;
        var computeSystem = computeSystemsResult.ComputeSystems.ToList().First();

        Assert.AreEqual(SupportedOperations, computeSystem.SupportedOperations);
    }
}
