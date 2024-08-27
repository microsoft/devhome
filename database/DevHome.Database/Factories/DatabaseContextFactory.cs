// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using Microsoft.Extensions.Hosting;

namespace DevHome.Database.Factories;

public class DatabaseContextFactory
{
    private readonly IHost _host;

    public DatabaseContextFactory(IHost host)
    {
        _host = host;
    }

    public DevHomeDatabaseContext GetNewDatabaseContext()
    {
        return new DevHomeDatabaseContext();
    }
}
