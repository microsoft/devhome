// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Logging;

namespace DevHome.Common.Helpers;

public class Log
{
    private static readonly ComponentLogger _logger = new("Common");

    public static Logger? Logger() => _logger.Logger;
}
