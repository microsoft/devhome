// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Common.Windows;

public sealed partial class Example2 : SecondaryWindow
{
    public string Example { get; set; } = "Example";

    public Example2()
    {
        this.InitializeComponent();
    }
}
