// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

internal class Log
{
    private static readonly ComponentLogger _logger = new ("ElevatedComponent");

    public static Logger? Logger => _logger.Logger;
}
