// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Stub.Helper;

using System;

internal interface IUpgradeHelper
{
    void StartUpgrade();

    double Progress
    {
        get;
    }

    UpgradeState State
    {
        get;
    }

    event EventHandler<UpgradeState> UpgradeStateChanged;

    event EventHandler<double> ProgressChanged;
}
