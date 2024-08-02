// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Data.Sqlite;

namespace DevHome.SetupFlow.DataModel;

public class DataStoreTransaction : IDataStoreTransaction
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private SqliteTransaction? _transaction;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    public static IDataStoreTransaction BeginTransaction(DataStore dataStore)
    {
        if (dataStore != null)
        {
            if (dataStore.Connection != null)
            {
                return new DataStoreTransaction(dataStore.Connection.BeginTransaction());
            }
        }

        return new DataStoreTransaction(null);
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private DataStoreTransaction(SqliteTransaction? tx)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        _transaction = tx;
    }

    public void Commit()
    {
        _transaction?.Commit();
    }

    public void Rollback()
    {
        _transaction?.Rollback();
    }

    private bool _disposed; // To detect redundant calls.

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _transaction = null;
            }

            _disposed = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
