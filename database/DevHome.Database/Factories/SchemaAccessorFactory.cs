// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using DevHome.Database.Services;

namespace DevHome.Database.Factories;

/// <summary>
/// Accessing the schema file differs between MSIX and winexe.
/// </summary>
public class SchemaAccessorFactory : ISchemaAccessFactory
{
    public ISchemaAccessor GenerateSchemaAccessor()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return new MSIXSchemaAccessor();
        }
        else
        {
            return new WinExeSchemaAccessor();
        }
    }
}
