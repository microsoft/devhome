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
/// This implementation is based on the code from the Windows Community Toolkit.
/// Reference: https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/winui/CommunityToolkit.WinUI.UI.Behaviors/Focus/AutoFocusBehavior.cs
/// Issue: https://github.com/CommunityToolkit/Windows/issues/443
/// </remarks>
public sealed class AutoFocusBehavior : BehaviorBase<Control>
{
    protected override void OnAssociatedObjectLoaded() => AssociatedObject.Focus(FocusState.Programmatic);
}
