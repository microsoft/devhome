// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.DataModel;

public interface IDataStoreTransaction : IDisposable
{
    void Commit();

    void Rollback();
}
