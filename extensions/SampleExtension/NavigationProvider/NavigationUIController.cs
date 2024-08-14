// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using Serilog;
using Windows.Foundation;

namespace SampleExtension.Providers;

internal sealed class NavigationUIController() : IExtensionAdaptiveCardSession
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(NavigationUIController)));

    private static ILogger Log => _logger.Value;

    private string? _template;

    private IExtensionAdaptiveCard? _navigationUI;

    public void Dispose()
    {
        Log.Debug($"Dispose");
        _navigationUI?.Update(null, null, null);
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        Log.Debug($"Initialize");
        _navigationUI = extensionUI;
        return UpdateCard();
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        return Task.Run(async () =>
        {
            try
            {
                Log.Information($"OnAction() called with {action}");
                Log.Debug($"inputs: {inputs}");
                var actionObject = JsonNode.Parse(action);
                var verb = actionObject?["verb"]?.GetValue<string>() ?? string.Empty;
                Log.Debug($"Verb: {verb}");
                switch (verb)
                {
                    case "OpenLogs":
                        FileHelper.OpenLogsLocation();
                        break;

                    default:
                        Log.Warning($"Unknown verb: {verb}");
                        break;
                }

                return UpdateCard();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected failure handling settings action.");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
            }
        }).AsAsyncOperation();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    private ProviderOperationResult UpdateCard()
    {
        try
        {
            var navigationCardData = new NavigationCardData
            {
            };

            return _navigationUI!.Update(
                GetTemplate(),
                JsonSerializer.Serialize(navigationCardData, NavigationCardSerializerContext.Default.NavigationCardData),
                "SettingsPage");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update settings card");
            return new ProviderOperationResult(ProviderOperationStatus.Failure, ex, ex.Message, ex.Message);
        }
    }

    private string GetTemplate()
    {
        if (_template is not null)
        {
            Log.Debug("Using cached template.");
            return _template;
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, @"SettingsProvider\SettingsCardTemplate.json");
            var template = File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
            template = Resources.ReplaceIdentifiers(template, GetSettingsCardResourceIdentifiers(), Log);
            Log.Debug($"Caching template");
            _template = template;
            return _template;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting template.");
            return string.Empty;
        }
    }

    private static string[] GetSettingsCardResourceIdentifiers()
    {
        return
        [
            "Settings_ViewLogs",
        ];
    }
}
