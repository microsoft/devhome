// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKApplyConfigurationResult
{
    public SDKApplyConfigurationSetResult ApplyResult { get; private set; }

    public SDKOpenConfigurationSetResult OpenResult { get; private set; }

    public ProviderOperationResult ProviderResult { get; private set; }

    public string ResultDescription { get; private set; }

    public SDKApplyConfigurationResult(ProviderOperationResult providerResult, SDKApplyConfigurationSetResult applyResult, SDKOpenConfigurationSetResult openResult)
    {
        ApplyResult = applyResult;
        OpenResult = openResult;
        ProviderResult = providerResult;
        ResultDescription = providerResult.DisplayMessage;
    }

    public bool ApplyConfigSucceeded => ApplyResult.Succeeded;

    public bool OpenConfigSucceeded => OpenResult.Succeeded;
}
