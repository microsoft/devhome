// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.SetupFlow.DataModel;

public interface IDataStoreSchema
{
    public long SchemaVersion
    {
        get;
    }

    public List<string> SchemaSqls
    {
        get;
    }
}
