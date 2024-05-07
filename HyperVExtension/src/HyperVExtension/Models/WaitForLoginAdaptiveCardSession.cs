// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using HyperVExtension.Common;
using HyperVExtension.Common.Extensions;
using HyperVExtension.CommunicationWithGuest;
using HyperVExtension.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel;
using Windows.Foundation;

namespace HyperVExtension.Models;

public sealed class WaitForLoginAdaptiveCardSession : IExtensionAdaptiveCardSession2, IDisposable
{
    private const int MaxAttempts = 3;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WaitForLoginAdaptiveCardSession));

    private sealed class InputPayload
    {
        public string? Id
        {
            get; set;
        }
    }

    private readonly IStringResource _stringResource;
    private readonly ApplyConfigurationOperation _operation;
    private readonly int _attemptNumber;
    private readonly ManualResetEvent _sessionStatusChangedEvent = new(false);
    private IExtensionAdaptiveCard? _extensionAdaptiveCard;
    private string? _template;
    private bool _isUserLoggedIn;
    private bool _disposed;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public WaitForLoginAdaptiveCardSession(IHost host, ApplyConfigurationOperation operation, int attemptNumber)
    {
        _stringResource = host.GetService<IStringResource>();
        _operation = operation;
        _attemptNumber = attemptNumber;
    }

    void IExtensionAdaptiveCardSession.Dispose()
    {
        ((IDisposable)this).Dispose();
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _extensionAdaptiveCard = extensionUI;
        int attemptNumberInText;
        bool showOkButton;
        string cancelText;
        string loginRequiredText;
        var loginRequiredText2 = string.Empty;
        string loginRequiredDescriptionText;
        var loginRequiredDescriptionText2 = string.Empty;
        string icon;
        if (_attemptNumber > MaxAttempts)
        {
            // If we exceeded number of attempts we'll show an error message in info bar with a dismiss button which
            // will return result as if user clicked cancel.
            attemptNumberInText = MaxAttempts;
            showOkButton = false;
            loginRequiredText = _stringResource.GetLocalized("WaitForLoginRequest/LoginRequiredTextAfterLastAttempt");
            loginRequiredText2 = _stringResource.GetLocalized("WaitForLoginRequest/LoginRequiredTextAfterLastAttempt2");
            loginRequiredDescriptionText = _stringResource.GetLocalized("WaitForLoginRequest/LoginRequiredDescriptionTextAfterLastAttempt");
            loginRequiredDescriptionText2 = _stringResource.GetLocalized("WaitForLoginRequest/LoginRequiredDescriptionTextAfterLastAttempt2");
            cancelText = _stringResource.GetLocalized("WaitForLoginRequest/DismissText");
            icon = ConvertIconToDataString("DarkError.png");
        }
        else
        {
            attemptNumberInText = _attemptNumber;
            showOkButton = true;
            loginRequiredText = _stringResource.GetLocalized("WaitForLoginRequest/LoginRequiredText");
            loginRequiredDescriptionText = _stringResource.GetLocalized("WaitForLoginRequest/LoginRequiredDescriptionText");
            cancelText = _stringResource.GetLocalized("WaitForLoginRequest/CancelText");
            icon = ConvertIconToDataString("DarkCaution.png");
        }

        var attemptCountText = _stringResource.GetLocalized("WaitForLoginRequest/AttemptCountText", attemptNumberInText, MaxAttempts);
        var title = _stringResource.GetLocalized("WaitForLoginRequest/Title");
        var description = _stringResource.GetLocalized("WaitForLoginRequest/Description");
        var okText = _stringResource.GetLocalized("WaitForLoginRequest/OkText");

        var dataJson = new JsonObject
        {
            { "attemptCountText", attemptCountText },
            { "title", title },
            { "description", description },
            { "loginRequiredText", loginRequiredText },
            { "loginRequiredText2", loginRequiredText2 },
            { "loginRequiredDescriptionText", loginRequiredDescriptionText },
            { "loginRequiredDescriptionText2", loginRequiredDescriptionText2 },
            { "okText", okText },
            { "cancelText", cancelText },
            { "attempt", _attemptNumber },
            { "showOkButton", showOkButton },
            { "icon", icon },
        };

        var operationResult = _extensionAdaptiveCard.Update(
            LoadTemplate(),
            dataJson.ToJsonString(),
            "WaitForVmUserLogin");

        return operationResult;
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(() =>
        {
            ProviderOperationResult operationResult;
            try
            {
                _log.Information($"OnAction() called with state:{_extensionAdaptiveCard?.State}");
                _log.Debug($"action: {action}");

                switch (_extensionAdaptiveCard?.State)
                {
                    case "WaitForVmUserLogin":
                        {
                            _log.Debug($"inputs: {inputs}");
                            var actionPayload = Helpers.Json.ToObject<AdaptiveCardActionPayload>(action) ?? throw new InvalidOperationException("Invalid action");
                            if (actionPayload.IsOkAction())
                            {
                                _isUserLoggedIn = true;
                            }

                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Success, null, null, null);
                            _sessionStatusChangedEvent.Set();
                            break;
                        }

                    default:
                        {
                            _log.Error($"Unexpected state:{_extensionAdaptiveCard?.State}");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Something went wrong", $"Unexpected state:{_extensionAdaptiveCard?.State}");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Exception in OnAction: {ex}");
                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, ex, "Something went wrong", ex.Message);
            }

            Stopped?.Invoke(this, new(operationResult, string.Empty));
            return operationResult;
        }).AsAsyncOperation();
    }

    public bool WaitForUserResponse()
    {
        WaitHandle.WaitAny(new[] { _sessionStatusChangedEvent, _operation.CancellationToken.WaitHandle });
        return _isUserLoggedIn;
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sessionStatusChangedEvent?.Dispose();
            }

            _disposed = true;
        }
    }

    private string LoadTemplate()
    {
        if (!string.IsNullOrEmpty(_template))
        {
            return _template;
        }

        var path = Path.Combine(Package.Current.EffectivePath, @"HyperVExtension\Templates\", "WaitForLoginAdaptiveCardTemplate.json");
        _template = File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
        return _template;
    }

    private static string ConvertIconToDataString(string fileName)
    {
        var path = Path.Combine(Package.Current.EffectivePath, @"HyperVExtension\Templates\", fileName);
        var imageData = Convert.ToBase64String(File.ReadAllBytes(path.ToString()));
        return imageData;
    }
}
