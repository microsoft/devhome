// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);
}
