// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Database.Services;

public interface ICustomMigrationHandlerFactory
{
    IReadOnlyList<ICustomMigration> GetCustomMigrationHandlers(uint previousSchemaVersion, uint currentSchemaVersion);
}
