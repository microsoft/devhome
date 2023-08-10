// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Logging;

public class GlobalLog
{
    public static Logger? Logger { get; } = new ComponentLogger("DevHome").Logger;
}
