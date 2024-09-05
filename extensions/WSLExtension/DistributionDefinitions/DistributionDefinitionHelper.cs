// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Text.Json;
using Serilog;
using Windows.Storage;
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
public class DistributionDefinitionHelper : IDistributionDefinitionHelper, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DistributionDefinitionHelper));

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly PackageHelper _packageHelper = new();

    private readonly Architecture _osArchitecture;

    private readonly SemaphoreSlim _definitionsLock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Dictionary<string, DistributionDefinition> _distributionDefinitionsMap = new();

    private bool _disposedValue;

    public DistributionDefinitionHelper(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _osArchitecture = RuntimeInformation.OSArchitecture;
    }

    /// <inheritdoc cref="IDistributionDefinitionHelper.GetDistributionDefinitionsAsync"/>
    public async Task<Dictionary<string, DistributionDefinition>> GetDistributionDefinitionsAsync()
    {
        await _definitionsLock.WaitAsync();

        try
        {
            // Get the update to date distribution definitions from WSL GitHub repository.
            // We use definitions from the web as our single source of truth, these web definitions are the
            // same that are used in the command wsl.exe --list --online.
            var client = _httpClientFactory.CreateClient();
            var distributionDefinitionsJson = await client.GetStringAsync(KnownDistributionsWebJsonLocation);
            var temp = new DistributionDefinitionsSourceGenerationContext(_jsonOptions);
            var webDefinitions = JsonSerializer.Deserialize(distributionDefinitionsJson, temp.DistributionDefinitions);

            foreach (var definition in webDefinitions!.Values)
            {
                // Only supported distributions for this machine.
                if (ShouldAddDistribution(definition))
                {
                    _distributionDefinitionsMap[definition.Name] = definition;
                }
            }

            // Merge the local distribution information we have stored in DistributionDefinition.yaml with the one above.
            var uri = new Uri(KnownDistributionsLocalYamlLocation);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var localYamlDefinitionsFile = await FileIO.ReadTextAsync(storageFile);
            var localYamlDefinitions = BuildYamlDeserializer().Deserialize<List<DistributionDefinition>>(localYamlDefinitionsFile);
            foreach (var localYamlDefinition in localYamlDefinitions)
            {
                // Ignore distributions that we have in the local yaml file but are no longer present in the web file.
                if (!_distributionDefinitionsMap.TryGetValue(localYamlDefinition.Name, out var definitionFromWeb))
                {
                    continue;
                }

                definitionFromWeb.Publisher = localYamlDefinition.Publisher;
                definitionFromWeb.WindowsTerminalProfileGuid = localYamlDefinition.WindowsTerminalProfileGuid;

                // Only add a logo to the definition we got from the web if the definition in the local yaml file
                // has one.
                if (!string.IsNullOrEmpty(localYamlDefinition.LogoFile))
                {
                    // Update the logo with the base64 string representation so we can show it as a thumbnail.
                    var logoFilePath = WslLogoPathFormat.FormatArgs(localYamlDefinition.LogoFile);
                    definitionFromWeb.Base64StringLogo = await _packageHelper.GetBase64StringFromLogoPathAsync(logoFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unable to retrieve all definitions for known distributions");
        }

        return _distributionDefinitionsMap;
    }

    private bool ShouldAddDistribution(DistributionDefinition distribution)
    {
        if (_osArchitecture == Architecture.Arm64)
        {
            return distribution.IsArm64Supported;
        }
        else if (_osArchitecture == Architecture.X64)
        {
            return distribution.IsAmd64Supported;
        }

        return false;
    }

    private IDeserializer BuildYamlDeserializer()
    {
        return new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _definitionsLock.Dispose();
            }

            _disposedValue = true;
        }
    }
}
