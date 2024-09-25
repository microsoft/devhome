// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.Services;

internal interface ISchemaValidator
{
    bool DoesPreviousSchemaExist();

    string GetPreviousSchema();

    void WriteSchemaVersion(uint schemaVersion);

    uint GetPreviousSchemaVersion(string schemaFileContents);
}
