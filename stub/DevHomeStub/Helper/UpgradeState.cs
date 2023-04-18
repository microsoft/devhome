// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Stub.Helper;

public enum UpgradeState
{
    Stopped = 0,
    InProgress = 1,
    NetworkError = 2,
    OtherError = 3,
    Downloading = 4,
    Deploying = 5,
    BlockedStoreError = 6,
}
