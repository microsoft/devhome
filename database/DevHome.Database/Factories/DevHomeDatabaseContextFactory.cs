// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Database.Services;
using Serilog;

namespace DevHome.Database.Factories;

public class DevHomeDatabaseContextFactory : IDevHomeDatabaseContextFactory
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeDatabaseContextFactory));

    public IDevHomeDatabaseContext GetNewContext()
    {
        _log.Information("Making a new DevHome Database Context");

        // Technically this one line method can be removed and instead all of DevHome
        // can call 'new DevHomeDatabaseContext'.  With a method all instantiations can be
        // controlled.  Leave this here in case other requirments are needed.
        return new DevHomeDatabaseContext();
    }
}
