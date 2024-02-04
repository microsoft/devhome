// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.SetupFlow.Exceptions;

/// <summary>
/// Exception thrown when the catalog is not initialized, likely because of a
/// failure to connect to the catalog.
/// </summary>
public class CatalogNotInitializedException : ArgumentNullException
{
}
