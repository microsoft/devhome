// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Environments.Models;

public class EnvironmentsCallToActionData
{
    public bool NavigateToExtensionsLibrary { get; }

    public string? CallToActionText { get; }

    public string? CallToActionHyperLinkText { get; }

    public EnvironmentsCallToActionData(bool navigateToExtensionsLibrary, string? callToActionText, string? callToActionHyperLinkText)
    {
        NavigateToExtensionsLibrary = navigateToExtensionsLibrary;
        CallToActionText = callToActionText;
        CallToActionHyperLinkText = callToActionHyperLinkText;
    }
}
