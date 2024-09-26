// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Serilog;

namespace DevHome.Database.Configurations;

public class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryConfiguration));

    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        try
        {
            builder.Property(x => x.ConfigurationFileLocation).HasDefaultValue(string.Empty);
            builder.Property(x => x.RepositoryClonePath).HasDefaultValue(string.Empty).IsRequired(true);
            builder.Property(x => x.RepositoryName).HasDefaultValue(string.Empty).IsRequired(true);
            builder.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
            builder.Property(x => x.UpdatedUTCDate).HasDefaultValueSql("datetime()");
            builder.Property(x => x.RepositoryUri).HasDefaultValue(string.Empty);
            builder.ToTable("Repository");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error building the repository data model.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryConfiguration_Event",
                LogLevel.Critical,
                new DatabaseContextErrorEvent("CreatingRepositoryModel", ex));
        }
    }
}
