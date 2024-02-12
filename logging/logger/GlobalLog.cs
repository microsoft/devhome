// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging;

public class GlobalLog
{
    public static Logger? Logger { get; } = new ComponentLogger("DevHome").Logger;
}
