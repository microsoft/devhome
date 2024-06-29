// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Streams;
using WSLExtension.Extensions;
using YamlDotNet.Serialization;
using static WSLExtension.Constants;

namespace WSLExtension.DistroDefinitions;

public static class KnownDistributionHelper
{
    public static async Task<Dictionary<string, KnownDistributionInfo>> GetKnownDistributionInfoFromYamlAsync()
    {
        try
        {
            var uri = new Uri(KnownDistributionYamlLocation);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var yamlString = await FileIO.ReadTextAsync(storageFile);
            var knownInfoList = BuildDeserializer().Deserialize<List<KnownDistributionInfo>>(yamlString);
            var knownDistributionMap = new Dictionary<string, KnownDistributionInfo>();

            foreach (var knownDistributionInfo in knownInfoList)
            {
                // Update the logo with the base64 string presentation before adding the distribution
                // to the map.
                var logoFilePath = WslLogoPathFormat.FormatArgs(knownDistributionInfo.LogoAssetName);
                knownDistributionInfo.Base64StringLogo = await GetBase64StringFromLogoPathAsync(logoFilePath);
                knownDistributionMap.Add(knownDistributionInfo.DistributionName, knownDistributionInfo);
            }

            return knownDistributionMap;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return new Dictionary<string, KnownDistributionInfo>();
        }
    }

    private static IDeserializer BuildDeserializer()
    {
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer;
    }

    public static async Task<string> GetBase64StringFromLogoPathAsync(string logoFilePath)
    {
        try
        {
            var uri = new Uri(logoFilePath);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var randomAccessStream = await storageFile.OpenReadAsync();

            // Convert the stream to a byte array
            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);

            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return string.Empty;
        }
    }
}
