// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using DevHome.SetupFlow.Contract.TaskOperator;

namespace DevHome.SetupFlow.TaskOperator;

public class ApplyConfigurationResult : IApplyConfigurationResult
{
    public IList<IConfigurationUnitResult> UnitResults { get; set; }

    public bool Attempted { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}

public class ConfigurationUnitResult : IConfigurationUnitResult
{
    public int HResult { get; set; }

    public string Intent { get; set; }

    public bool IsSkipped { get; set; }

    public string UnitName { get; set; }
}
