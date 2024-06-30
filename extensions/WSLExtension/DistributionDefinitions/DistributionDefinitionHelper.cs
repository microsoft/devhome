// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using WSLExtension.ClassExtensions;
using YamlDotNet.Serialization;
using static WSLExtension.Constants;

namespace WSLExtension.DistributionDefinitions;

public static class DistributionDefinitionHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<Dictionary<string, DistributionDefinition>> GetDistributionDefinitionsAsync()
    {
        try
        {
            // Get the update to date distribution information from WSL GitHub repository.
            using var client = new HttpClient();
            var distributionDefinitionsJson = await client.GetStringAsync(KnownDistributionsWebJsonLocation);
            var webDefinitions = JsonSerializer.Deserialize<DistributionDefinitions>(distributionDefinitionsJson, _jsonOptions);
            var distributionDefinitionsMap = new Dictionary<string, DistributionDefinition>();

            foreach (var distributionInfo in webDefinitions!.Values)
            {
                // filter out unsupported distributions for this machine.
                if (ShouldAddDistribution(distributionInfo))
                {
                    distributionDefinitionsMap.Add(distributionInfo.Name, distributionInfo);
                }
            }

            // Merge the local distribution information we have stored in DistributionDefinition.yaml with the one above.
            var uri = new Uri(KnownDistributionsLocalYamlLocation);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var yamlDefinitions = await FileIO.ReadTextAsync(storageFile);
            var localDefinitions = BuildDeserializer().Deserialize<List<DistributionDefinition>>(yamlDefinitions);
            foreach (var localDistributionInfo in localDefinitions)
            {
                if (distributionDefinitionsMap.TryGetValue(localDistributionInfo.Name, out var distributionInfo))
                {
                    // Update the logo with the base64 string representation so we can show it as a thumbnail.
                    var logoFilePath = WslLogoPathFormat.FormatArgs(localDistributionInfo.LogoFile);
                    distributionInfo.Base64StringLogo = await GetBase64StringFromLogoPathAsync(logoFilePath);
                }
            }

            return distributionDefinitionsMap;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return new Dictionary<string, DistributionDefinition>();
        }
    }

    private static bool ShouldAddDistribution(DistributionDefinition distribution)
    {
        var arch = RuntimeInformation.OSArchitecture;
        if (arch == Architecture.Arm64)
        {
            return distribution.IsArm64Supported;
        }
        else if (arch == Architecture.X64)
        {
            return distribution.IsAmd64Supported;
        }

        return false;
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
            Console.WriteLine(ex.ToString(), logoFilePath);
            return string.Empty;
        }
    }
}
