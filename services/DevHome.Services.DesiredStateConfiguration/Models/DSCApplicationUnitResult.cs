// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;
using Windows.Win32.Foundation;

namespace DevHome.Services.DesiredStateConfiguration.Models;

internal sealed class DSCApplicationUnitResult : IDSCApplicationUnitResult
{
    public DSCApplicationUnitResult(ApplyConfigurationUnitResult unitResult)
    {
        // Constructor copies all the required data from the out-of-proc COM
        // objects over to the current process. This ensures that we have this
        // information available even if the out-of-proc COM objects are no
        // longer available (e.g. AppInstaller service is no longer running).
        AppliedUnit = new DSCUnit(unitResult.Unit);
        IsSkipped = unitResult.State == ConfigurationUnitState.Skipped;
        HResult = unitResult.ResultInformation?.ResultCode?.HResult ?? HRESULT.S_OK;
        ResultSource = unitResult.ResultInformation?.ResultSource ?? ConfigurationUnitResultSource.None;
        ErrorDescription = unitResult.ResultInformation?.Description;
        Details = unitResult.ResultInformation?.Details;
        RebootRequired = unitResult.RebootRequired;
    }

    public IDSCUnit AppliedUnit { get; }

    public string ErrorDescription { get; }

    public bool RebootRequired { get; }

    public bool IsSkipped { get; }

    public int HResult { get; }

    public ConfigurationUnitResultSource ResultSource { get; }

    public string Details { get; }
}
