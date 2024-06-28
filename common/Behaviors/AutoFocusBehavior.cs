// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Behaviors;

/// <summary>
/// This behavior automatically sets the focus on the associated <see cref="Control"/> when it is loaded.
/// </summary>
/// <remarks>
/// This implementation is based on the Uwp code from the Windows Community Toolkit.
/// https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/Microsoft.Toolkit.Uwp.UI.Behaviors/Focus/AutoFocusBehavior.cs
/// </remarks>
public sealed class AutoFocusBehavior : BehaviorBase<Control>
{
    protected override void OnAssociatedObjectLoaded() => AssociatedObject.Focus(FocusState.Programmatic);
}
