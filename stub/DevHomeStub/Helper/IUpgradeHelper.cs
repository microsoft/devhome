//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------

namespace Microsoft.DevHome.Stub.Helper
{
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
}
