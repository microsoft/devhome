// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Serilog;

namespace DevHome.PI.Helpers;

internal sealed class ErrorLookupHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ErrorLookupHelper));

    private static SqliteConnectionStringBuilder? _dbConnectionString;

    private static SqliteConnectionStringBuilder DbConnectionString
    {
        get
        {
            if (_dbConnectionString == null)
            {
                _dbConnectionString = new SqliteConnectionStringBuilder();
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                var dbPath = Path.Combine(path ?? string.Empty, "errors.db");

                _dbConnectionString.DataSource = dbPath;
                _dbConnectionString.Mode = SqliteOpenMode.ReadOnly;
            }

            return _dbConnectionString;
        }
    }

    public static AppError[]? LookupError(int error)
    {
        try
        {
            using SqliteConnection connection = new(DbConnectionString.ConnectionString);
            connection.Open();
            AppError[]? errors = LookupErrors(connection, error);
            connection.Close();
            return errors;
        }
        catch
        {
            _log.Error("Failed to look up errors: {AppError}", error.ToString(CultureInfo.CurrentCulture));
        }

        return null;
    }

    private static AppError[]? LookupErrors(SqliteConnection connection, int hresult)
    {
        // Look up a solution for an error.
        SqliteCommand errorCommand = connection.CreateCommand();
        errorCommand.CommandText = @"select Name, Help from tblErrors WHERE code=@code";

        SqliteParameter errorParam = new("@code", hresult.ToString(CultureInfo.CurrentCulture));
        errorCommand.Parameters.Add(errorParam);
        SqliteDataReader errorReader = errorCommand.ExecuteReader();
        IList<AppError> errors = [];

        while (errorReader.Read())
        {
            AppError error = new()
            {
                Code = hresult,
                Name = errorReader.GetString(0),
                Help = errorReader.GetString(1),
            };
            errors.Add(error);
        }

        errorReader.Close();
        return errors.ToArray();
    }
}

public class AppError
{
    public int Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Help { get; set; } = string.Empty;
}
