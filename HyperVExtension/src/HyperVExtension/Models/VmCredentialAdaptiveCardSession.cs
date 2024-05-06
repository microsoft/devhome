// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Net;
using System.Security;
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

public sealed class VmCredentialAdaptiveCardSession : IExtensionAdaptiveCardSession2, IDisposable
{
    private const int MaxAttempts = 3;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VmCredentialAdaptiveCardSession));

    private sealed class InputPayload
    {
        public string? Id
        {
            get; set;
        }

        public string? UserVal
        {
            get; set;
        }

        public string? PassVal
        {
            get; set;
        }
    }

    private readonly IStringResource _stringResource;
    private readonly ApplyConfigurationOperation _operation;
    private readonly int _attemptNumber;
    private readonly ManualResetEvent _sessionStatusChangedEvent = new(false);
    private IExtensionAdaptiveCard? _extensionAdaptiveCard;
    private string? _usernameString;
    private SecureString? _passwordString;
    private string? _template;
    private bool _disposed;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public VmCredentialAdaptiveCardSession(IHost host, ApplyConfigurationOperation operation, int attemptNumber)
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
        var showInfobar = _attemptNumber > 1;
        int attemptNumberInText;
        bool showOkButton;
        string cancelText;
        string invalidCredentialText;
        string invalidCredentialDescription;
        if (_attemptNumber > MaxAttempts)
        {
            // If we exceeded number of attempts we'll show an error message in info bar with a dismiss button which
            // will return result as if user clicked cancel.
            attemptNumberInText = MaxAttempts;
            showOkButton = false;
            invalidCredentialText = _stringResource.GetLocalized("VmCredentialRequest/InvalidCredentialTextAfterLastAttempt");
            invalidCredentialDescription = _stringResource.GetLocalized("VmCredentialRequest/InvalidCredentialDescriptionAfterLastAttempt");
            cancelText = _stringResource.GetLocalized("VmCredentialRequest/DismissText");
        }
        else
        {
            attemptNumberInText = _attemptNumber;
            showOkButton = true;
            invalidCredentialText = _stringResource.GetLocalized("VmCredentialRequest/InvalidCredentialText");
            invalidCredentialDescription = _stringResource.GetLocalized("VmCredentialRequest/InvalidCredentialDescription");
            cancelText = _stringResource.GetLocalized("VmCredentialRequest/CancelText");
        }

        var attemptCountText = _stringResource.GetLocalized("VmCredentialRequest/AttemptCountText", attemptNumberInText, MaxAttempts);
        var title = _stringResource.GetLocalized("VmCredentialRequest/Title");
        var description = _stringResource.GetLocalized("VmCredentialRequest/Description");
        var userNameLabel = _stringResource.GetLocalized("VmCredentialRequest/UsernameLabel");
        var passwordLabel = _stringResource.GetLocalized("VmCredentialRequest/PasswordLabel");
        var userNameIsRequiredText = _stringResource.GetLocalized("VmCredentialRequest/UserNameIsRequiredText");
        var okText = _stringResource.GetLocalized("VmCredentialRequest/OkText");

        var dataJson = new JsonObject
        {
            { "attemptCountText", attemptCountText },
            { "title", title },
            { "description", description },
            { "userNameLabel", userNameLabel },
            { "passwordLabel", passwordLabel },
            { "userNameIsRequiredText", userNameIsRequiredText },
            { "invalidCredentialText", invalidCredentialText },
            { "invalidCredentialDescription", invalidCredentialDescription },
            { "okText", okText },
            { "cancelText", cancelText },
            { "showInfobar", showInfobar },
            { "showOkButton", showOkButton },
            { "icon", ConvertIconToDataString("DarkError.png") },
        };

        var operationResult = _extensionAdaptiveCard.Update(
            LoadTemplate(),
            dataJson.ToJsonString(),
            "VmCredential");

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
                    case "VmCredential":
                        {
                            _log.Debug($"inputs: {inputs}");
                            var actionPayload = Helpers.Json.ToObject<AdaptiveCardActionPayload>(action) ?? throw new InvalidOperationException("Invalid action");
                            if (actionPayload.IsOkAction())
                            {
                                var inputPayload = Helpers.Json.ToObject<InputPayload>(inputs) ?? throw new InvalidOperationException("Invalid inputs");
                                _usernameString = inputPayload.UserVal;
                                _passwordString = new NetworkCredential(string.Empty, inputPayload.PassVal).SecurePassword;
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

    public (string? userName, SecureString? password) WaitForCredentials()
    {
        WaitHandle.WaitAny([_sessionStatusChangedEvent, _operation.CancellationToken.WaitHandle]);
        return (_usernameString, _passwordString);
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

        var path = Path.Combine(Package.Current.EffectivePath, @"HyperVExtension\Templates\", "VmCredentialAdaptiveCardTemplate.json");
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
