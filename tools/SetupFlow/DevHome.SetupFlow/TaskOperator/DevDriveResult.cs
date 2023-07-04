﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Contract.TaskOperator;

namespace DevHome.SetupFlow.TaskOperator;
public class CreateDevDriveResult : ICreateDevDriveResult
{
    public int HResult { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}
