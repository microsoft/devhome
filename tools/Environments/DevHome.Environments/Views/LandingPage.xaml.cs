// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common;

namespace DevHome.Environments.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LandingPage : ToolPage
{
    public LandingPage()
    {
        this.InitializeComponent();
    }

    public override string ShortName => "Environments";
}
