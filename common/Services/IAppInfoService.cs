// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.Common.Services;

public interface IAppInfoService
{
    public string GetAppNameLocalized();

    public Version GetAppVersion();
}
