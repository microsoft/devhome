// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Test.Environments.Models;

/// <summary>
/// Test class that implements IDeveloperId.
/// </summary>
public class TestDeveloperId : IDeveloperId
{
    public string LoginId => "TestDeveloperId@contoso.com";

    public string Url => "www.contosoTesting.contoso.com";
}
