// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;

namespace DevHome.SetupFlow.Common.Helpers;

#nullable enable

public class Log
{
    private static readonly ComponentLogger _logger = new ("SetupFlow");

    public static Logger? Logger => _logger.Logger;
}
