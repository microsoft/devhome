// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Common.Services;

public interface IAppInfoService
{
    public string GetAppNameLocalized();

    public Version GetAppVersion();

    public string IconPath { get; }
}
