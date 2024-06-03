// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Services;

public interface IStringResource
{
    public string GetLocalized(string key, params object[] args);

    public string GetResourceFromPackage(string resource, string packageFullName);
}
