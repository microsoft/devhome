// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

/// <summary>
/// Class for the result of an individual unit/resource inside <see cref="ElevatedConfigureTaskResult"/>
/// </summary>
public sealed class ElevatedConfigureUnitTaskResult
{
    private readonly IDSCApplicationUnitResult _unitResult;

    public ElevatedConfigureUnitTaskResult(IDSCApplicationUnitResult unitResult)
    {
        _unitResult = unitResult;
    }

    public string? Type => _unitResult.Type;

    public string? Id => _unitResult.Id;

    public string? UnitDescription => _unitResult.UnitDescription;

    public string? Intent => _unitResult.Intent;

    public bool IsSkipped => _unitResult.IsSkipped;

    public int HResult => _unitResult.HResult;

    public int ResultSource => (int)_unitResult.ResultSource;

    public string? Details => _unitResult.Details;

    public string? ErrorDescription => _unitResult.ErrorDescription;
}
