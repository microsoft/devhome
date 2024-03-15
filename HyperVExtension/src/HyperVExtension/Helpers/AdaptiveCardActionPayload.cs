// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace HyperVExtension.Helpers;

internal sealed class AdaptiveCardActionPayload
{
    public string? Id
    {
        get; set;
    }

    public string? Style
    {
        get; set;
    }

    public string? ToolTip
    {
        get; set;
    }

    public string? Title
    {
        get; set;
    }

    public string? Type
    {
        get; set;
    }

    public bool IsCancelAction()
    {
        return Id == "cancelAction";
    }

    public bool IsOkAction()
    {
        return Id == "okAction";
    }

    public bool IsUrlAction()
    {
        return Type == "Action.OpenUrl";
    }

    public bool IsSubmitAction()
    {
        return Type == "Action.Submit";
    }

    public bool IsExecuteAction()
    {
        return Type == "Action.Execute";
    }
}
