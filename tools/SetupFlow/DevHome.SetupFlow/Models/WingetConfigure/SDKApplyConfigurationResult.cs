// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKApplyConfigurationResult
{
    public SDKApplyConfigurationSetResult ApplyResult { get; private set; }

    public SDKOpenConfigurationSetResult OpenResult { get; private set; }

    public Exception ResultCode { get; private set; }

    public string ResultDescription { get; private set; }

    public SDKApplyConfigurationResult(Exception resultCode, string resultDescription, SDKApplyConfigurationSetResult applyResult, SDKOpenConfigurationSetResult openResult)
    {
        ApplyResult = applyResult;
        OpenResult = openResult;
        ResultCode = resultCode;
        ResultDescription = resultDescription;
    }

    public bool ApplyConfigSucceeded => ApplyResult.Succeeded;

    public bool OpenConfigSucceeded => OpenResult.Succeeded;
}
