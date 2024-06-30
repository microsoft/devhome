// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Contracts;

public interface IStringResource
{
    public string GetLocalized(string key, params object[] args);
}
