// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// ErrorDBPopulateTool is a tool that populates a SQLite database with error codes and descriptions from HRESULTS.txt and Win32Errors.txt.
//
// usage: ErrorDBPopulateTool
//
// The tool expects the following files to be in the same directory as the executable:
// - HRESULTS.txt: A text file containing HRESULT error codes and descriptions.
// - Win32Errors.txt: A text file containing Win32 error codes and descriptions.
//
// The tool will create a SQLite database file named "errors.db" in the same directory as the executable.
// The database will contain a table named "tblErrors" with the following schema:
// - ErrorCode: An integer column containing the error code.
// - Name: A text column containing the error name.
// - Description: A text column containing the error description.
using System.Globalization;
using Microsoft.Data.Sqlite;

var createTableQuery = @"
    CREATE TABLE IF NOT EXISTS tblErrors(
    code  INTEGER,
    Name  TEXT,
    Help  TEXT);";

// Make sure we're operating on a fresh database
if (File.Exists("errors.db"))
{
    File.Delete("errors.db");
}

SqliteConnectionStringBuilder connectionString = new SqliteConnectionStringBuilder();
connectionString.DataSource = "errors.db";

using (SqliteConnection connection = new SqliteConnection(connectionString.ConnectionString))
{
    connection.Open();

    // Populate the database in one transaction to dramatically improve performance
    var transaction = connection.BeginTransaction();

    // Create the table
    var createTableCommand = connection.CreateCommand();
    createTableCommand.CommandText = createTableQuery;
    createTableCommand.ExecuteNonQuery();

    // Populate the table
    string[] filenames = { "HRESULTS.txt", "Win32Errors.txt", "NtStatus.txt" };

    foreach (var filename in filenames)
    {
        // These files are formatted as follows:
        // <Hex Number> <newline> <error name> <tab> <Description> <newline>
        var data = File.ReadAllText(filename);
        var lines = data.Split("\r\n");
        var converter = new System.ComponentModel.Int32Converter();

        for (var i = 0; i < lines.Length; i += 2)
        {
            var errorAsHex = lines[i].Trim();
            var errorInfo = lines[i + 1].Trim().Split('\t');

            var errorAsDecimal = ((int)(converter.ConvertFromString(errorAsHex) ?? throw new InvalidDataException())).ToString(CultureInfo.InvariantCulture);
            var name = errorInfo[0];
            var description = errorInfo[1];

            Console.WriteLine("(" + errorAsDecimal + ") " + name + ": " + description);

            AddError(connection, errorAsDecimal, name, description);
        }
    }

    transaction.Commit();
}

void AddError(SqliteConnection connection, string error, string errorName, string errorDescription)
{
    var command = connection.CreateCommand();

    command.CommandText = @"insert into tblErrors values(@ErrorCode, @Name, @Description);";

    SqliteParameter param1 = new SqliteParameter("@ErrorCode", error);
    command.Parameters.Add(param1);
    SqliteParameter param2 = new SqliteParameter("@Name", errorName);
    command.Parameters.Add(param2);
    SqliteParameter param3 = new SqliteParameter("@Description", errorDescription);
    command.Parameters.Add(param3);

    command.ExecuteNonQuery();
}
