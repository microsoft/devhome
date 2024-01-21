// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;

namespace DevHome.Common.Helpers;

public class Log
{
    private static readonly ComponentLogger _logger = new("Common");

    public static Logger? Logger() => _logger.Logger;
}
