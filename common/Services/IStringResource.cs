// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Common.Services;

public interface IStringResource
{
    public string GetLocalized(string key, params object[] args);
}
