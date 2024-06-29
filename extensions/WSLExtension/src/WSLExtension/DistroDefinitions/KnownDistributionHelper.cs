// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Storage;
using YamlDotNet.Serialization;

namespace WSLExtension.DistroDefinitions;

public static class KnownDistributionHelper
{
    public static async Task<Dictionary<string, KnownDistributionInfo>> RetrieveKnownDistributionInfoAsync()
    {
        var deserializer = BuildDeserializer();
        try
        {
            var uri = new Uri("ms-appx:///WSLExtension/DistroDefinitions/KnownDistributionInfo.yaml");
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var yamlString = await FileIO.ReadTextAsync(storageFile);
            var knownInfoList = deserializer.Deserialize<List<KnownDistributionInfo>>(yamlString);
            var knownDistributionMap = new Dictionary<string, KnownDistributionInfo>();

            foreach (var knownDistributionInfo in knownInfoList)
            {
                knownDistributionMap.Add(knownDistributionInfo.DistributionName, knownDistributionInfo);
            }

            return knownDistributionMap;
        }
        catch
        {
            return new Dictionary<string, KnownDistributionInfo>();
        }
    }

    public static IDeserializer BuildDeserializer()
    {
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer;
    }

    public static ISerializer BuildSerializer()
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();
        return serializer;
    }
}
