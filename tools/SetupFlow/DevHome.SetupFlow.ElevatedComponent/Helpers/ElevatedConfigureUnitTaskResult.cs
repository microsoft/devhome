// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

/// <summary>
/// Class for the result of an individual unit/resource inside <see cref="ElevatedConfigureTaskResult"/>
/// </summary>
public sealed class ElevatedConfigureUnitTaskResult
{
    private readonly IDSCApplicationUnitResult? _unitResult;

    public ElevatedConfigureUnitTaskResult()
    {
        // This constructor is required for CsWinRT projection.
    }

    internal ElevatedConfigureUnitTaskResult(IDSCApplicationUnitResult unitResult)
    {
        _unitResult = unitResult;
    }

    public string? Type => _unitResult?.AppliedUnit.Type;

    public string? Id => _unitResult?.AppliedUnit.Id;

    public string? UnitDescription => _unitResult?.AppliedUnit.Description;

    public string? Intent => _unitResult?.AppliedUnit.Intent;

    public bool IsSkipped => _unitResult?.IsSkipped ?? false;

    public int HResult => _unitResult?.HResult ?? HRESULT.S_OK;

    public int ResultSource => (int)(_unitResult?.ResultSource ?? ConfigurationUnitResultSource.None);

    public string? Details => _unitResult?.Details;

    public string? ErrorDescription => _unitResult?.ErrorDescription;
}
