// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using WSLExtension.Models;

namespace WSLExtension.Services;

public interface IProcessCreator
{
    public Process CreateProcessWithWindow(string fileName, string arguments);

    public WslProcessData CreateProcessWithoutWindow(string fileName, string arguments);
}
