// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevHome.Database.Services;

namespace DevHome.Database.Factories;

public class CustomMigrationHandlerFactory : ICustomMigrationHandlerFactory
{
    /// <summary>
    /// Gets a list of all migrations that can handle the migration from previousSchemaVersion to
    /// currentSchemaVersion in priority order (min to max).
    /// </summary>
    /// <param name="previousSchemaVersion">The schema version upgrading from.</param>
    /// <param name="currentSchemaVersion">The schema version upgrading to.</param>
    /// <remarks>Specifically, this grabs all classes the inherit from ICustomMigration that is not
    /// abstract, not generic, and has a parameterless constructor.</remarks>
    /// <returns>A list of all objects that can handle the migration, sorted in priority order.</returns>
    public IReadOnlyList<ICustomMigration> GetCustomMigrationHandlers(uint previousSchemaVersion, uint currentSchemaVersion)
    {
        var customLogics = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(ICustomMigration).IsAssignableFrom(type))
            .Where(type =>
                !type.IsAbstract &&
                !type.IsGenericType &&
                type.GetConstructor(Array.Empty<Type>()) != null)
            .Select(type => (ICustomMigration)Activator.CreateInstance(type)!)
            .OrderBy(x => x.Priority)
            .ToList();
        return customLogics;
    }
}
