// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
public partial class ShimmerSearchViewModel : ObservableObject
{
    public IEnumerable<int> ResultPackageShimmers { get; } = Enumerable.Range(1, 10);
}
