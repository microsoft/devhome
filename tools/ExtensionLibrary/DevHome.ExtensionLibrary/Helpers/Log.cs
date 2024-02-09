// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Logging;

namespace DevHome.ExtensionLibrary.Helpers;

public class Log
{
    private static readonly ComponentLogger _logger = new("ExtensionLibrary");

    public static Logger? Logger() => _logger.Logger;
}
