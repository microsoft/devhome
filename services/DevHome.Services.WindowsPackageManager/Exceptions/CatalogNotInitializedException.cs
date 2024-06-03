// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Services.WindowsPackageManager.Exceptions;

/// <summary>
/// Exception thrown when the catalog is not initialized, likely because of a
/// failure to connect to the catalog.
/// </summary>
public class CatalogNotInitializedException : ArgumentNullException
{
}
