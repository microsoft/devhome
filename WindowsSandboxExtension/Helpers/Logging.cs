// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace WindowsSandboxExtension.Helpers;

public class Logging
{
    public static readonly string LogExtension = ".dhlog";

    public static readonly string LogFolderName = "Logs";

    public static readonly string DefaultLogFileName = "wsb";

    private static readonly Lazy<string> _logFolderRoot = new(() => Path.Combine(ApplicationData.Current.TemporaryFolder.Path, LogFolderName));

    public static readonly string LogFolderRoot = _logFolderRoot.Value;
}
