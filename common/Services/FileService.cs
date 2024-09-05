// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DevHome.Common.Contracts;

namespace DevHome.Common.Services;

public class FileService : IFileService
{
#pragma warning disable CS8603 // Possible null reference return.
    public T Read<T>(string folderPath, string fileName, JsonTypeInfo<T> jsonTypeInfo)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            using var fileStream = File.OpenText(path);
            return JsonSerializer.Deserialize<T>(fileStream.BaseStream, jsonTypeInfo);
        }

        return default;
    }
#pragma warning restore CS8603 // Possible null reference return.

    public void Save<T>(string folderPath, string fileName, T content, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonSerializer.Serialize(content, jsonTypeInfo);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}
