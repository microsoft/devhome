// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);
}
