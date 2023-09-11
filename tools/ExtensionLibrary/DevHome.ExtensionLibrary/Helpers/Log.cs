// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;

namespace DevHome.Dashboard.Helpers;

public class Log
{
    private static readonly ComponentLogger _logger = new ("ExtensionLibrary");

    public static Logger? Logger() => _logger.Logger;
}
