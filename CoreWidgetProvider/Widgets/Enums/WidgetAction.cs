// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace CoreWidgetProvider.Widgets.Enums;
public enum WidgetAction
{
    /// <summary>
    /// Error condition where the action cannot be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Action to validate the path provided by the user.
    /// </summary>
    CheckPath,

    /// <summary>
    /// Action to connect to host.
    /// </summary>
    Connect,

    /// <summary>
    /// Action to show previous widget item.
    /// </summary>
    PrevItem,

    /// <summary>
    /// Action to show next widget item.
    /// </summary>
    NextItem,
}
