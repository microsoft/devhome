// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCApplicationUnitResult
{
    public string Type { get; }

    public string Id { get; }

    public string UnitDescription { get; }

    public string ErrorDescription { get; }

    public bool RebootRequired { get; }

    public string Intent { get; }

    public bool IsSkipped { get; }

    public int HResult { get; }

    public ConfigurationUnitResultSource ResultSource { get; }

    public string Details { get; }
}
