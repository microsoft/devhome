// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.SetupFlow.Exceptions;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.WingetConfigure;

public class SDKApplyConfigurationSetResult
{
    public SDK.ApplyConfigurationSetResult Result
    {
        get;
    }

    public bool Succeeded { get; private set; }

    public bool RequiresReboot => Result?.UnitResults?.Any(result => result.RebootRequired) ?? false;

    public Exception ResultException { get; private set; }

    public SDKApplyConfigurationSetResult(SDK.ApplyConfigurationSetResult result)
    {
        Result = result;
        var isResultSetNull = Result == null;
        Succeeded = !isResultSetNull && (Result.ResultCode == null || Result.ResultCode?.HResult == 0);

        if (isResultSetNull)
        {
            ResultException = new SDKApplyConfigurationSetResultException("Unable to get the result of the applied configuration as it was null.");
        }
        else
        {
            ResultException = Result.ResultCode;
        }
    }

    public bool AreConfigUnitsAvailable => Result?.UnitResults != null && Result.UnitResults.Count > 0;
}
