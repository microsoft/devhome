// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Helpers;
using Windows.Storage;

namespace DevHome.Database;

internal static class SchemaHelper
{
    internal const string SchemaVersionDirectoryPath = "Assets";

    internal const string SchemaVersionFileName = "SchemaVersion.txt";
}
