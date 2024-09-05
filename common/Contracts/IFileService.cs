// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization.Metadata;

namespace DevHome.Common.Contracts;

public interface IFileService
{
    T Read<T>(string folderPath, string fileName, JsonTypeInfo<T> jsonTypeInfo);

    void Save<T>(string folderPath, string fileName, T content, JsonTypeInfo<T> jsonTypeInfo);

    void Delete(string folderPath, string fileName);
}
