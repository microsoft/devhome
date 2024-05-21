// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Models;

internal sealed class RestoreState
{
    internal double Left { get; set; }

    internal double Top { get; set; }

    internal double Width { get; set; }

    internal double Height { get; set; }

    internal Orientation BarOrientation { get; set; }

    internal bool IsLargePanelVisible { get; set; }
}
