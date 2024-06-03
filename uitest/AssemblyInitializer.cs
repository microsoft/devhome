// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.UITest.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevHome.UITest;

[TestClass]
public class AssemblyInitializer
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        DevHomeApplication.Instance.Initialize(context.Properties["AppSettingsMode"].ToString());
    }
}
