// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using WSLExtension.ClassExtensions;
using WSLExtension.Contracts;
using WSLExtension.Helpers;
using YamlDotNet.Serialization;
using static WSLExtension.Constants;

namespace WSLExtension.DistributionDefinitions;

/// <summary>
/// Provides definition information about all the WSL distributions that can be found at
/// <see cref="Constants.KnownDistributionsWebJsonLocation"/>.
/// </summary>
public class DistributionDefinitionHelper : IDistributionDefinitionHelper
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly PackageHelper _packageHelper = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DistributionDefinitionHelper(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc cref="IDistributionDefinitionHelper.GetDistributionDefinitionsAsync"/>
    public async Task<Dictionary<string, DistributionDefinition>> GetDistributionDefinitionsAsync()
    {
        try
        {
            // Get the update to date distribution information from WSL GitHub repository.
            var client = _httpClientFactory.CreateClient();
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
            var localDefinitions = BuildYamlDeserializer().Deserialize<List<DistributionDefinition>>(yamlDefinitions);
            foreach (var localDistributionInfo in localDefinitions)
            {
                if (!distributionDefinitionsMap.TryGetValue(localDistributionInfo.Name, out var webDistributionInfo))
                {
                    continue;
                }

                // Ignore distributions in the local file like oracle Linux without an image.
                if (string.IsNullOrEmpty(localDistributionInfo.LogoFile))
                {
                    continue;
                }

                // Update the logo with the base64 string representation so we can show it as a thumbnail.
                var logoFilePath = WslLogoPathFormat.FormatArgs(localDistributionInfo.LogoFile);
                webDistributionInfo.Base64StringLogo = await _packageHelper.GetBase64StringFromLogoPathAsync(logoFilePath);
            }

            return distributionDefinitionsMap;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return new Dictionary<string, DistributionDefinition>();
        }
    }

    private bool ShouldAddDistribution(DistributionDefinition distribution)
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

    private IDeserializer BuildYamlDeserializer()
    {
        return new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
    }
}
