// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging;

namespace DevHome.SetupFlow.Common.Helpers;

#nullable enable

public class Log
{
    private static readonly ComponentLogger _logger = new ("SetupFlow");

    public static Logger? Logger => _logger.Logger;

    // Component names to prepend to log strings
    public static class Component
    {
        public static readonly string AppManagement = nameof(AppManagement);
        public static readonly string Configuration = nameof(Configuration);
        public static readonly string DevDrive = nameof(DevDrive);
        public static readonly string Loading = nameof(Loading);
        public static readonly string MainPage = nameof(MainPage);
        public static readonly string Orchestrator = nameof(Orchestrator);
        public static readonly string RepoConfig = nameof(RepoConfig);
    }
}
