// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.PI.Models;

public partial class FrameworkType : ObservableObject
{
    internal bool IsExactMatch { get; private set; } = true;

    internal string Determinator
    {
        get; set;
    }

    public string Name
    {
        get; set;
    }

    [ObservableProperty]
    private bool isTypeSupported;

    public FrameworkType(string determinator, string name, bool isExactMatch = true)
    {
        Determinator = determinator;
        Name = name;
        IsExactMatch = isExactMatch;
    }
}
