// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Test.Environments.Helpers;

public class TestHelpers
{
    // Test Id for the test compute system.
    public static string ComputeSystemId => "95E9E2BA-C51C-4FD1-BF08-A5D75EC65004";

    public static string ComputeSystemName => "TestComputeSystem-" + ComputeSystemId;

    public static string ComputeSystemAlternativeDisplayName => "AlternativeName-" + ComputeSystemName;

    public static string ComputeSystemProviderId => "Contoso.CloudVMProduct.ComputeSystem";

    public static string ComputeSystemProviderDisplayName => "TestComputeSystemProvider";
}
