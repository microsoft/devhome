// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Common;

public interface IStringResource
{
    public string GetLocalized(string key, params object[] args);
}
