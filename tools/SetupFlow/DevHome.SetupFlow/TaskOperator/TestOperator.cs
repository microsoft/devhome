// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Contract.TaskOperator;

namespace DevHome.SetupFlow.TaskOperator;
public class TestOperator : ITestOperator
{
    public void WriteToStdOut(string msg) => Console.WriteLine(msg);
}
