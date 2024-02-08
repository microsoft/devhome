// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace HyperVExtension.Common;

public interface IStringResource
{
    public string GetLocalized(string key, params object[] args);
}
