// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;

namespace DevHome.Database.Services;

internal class SchemaValidatorFactory
{
    internal static ISchemaValidator MakeValidator()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return new MSIXSchemaValidator();
        }
        else
        {
            return new WinExeSchemaValidator();
        }
    }
}
