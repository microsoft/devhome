// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Security;
using HyperVExtension.CommunicationWithGuest;
using HyperVExtension.Helpers;
using HyperVExtension.Providers;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace HyperVExtension.Models;

public sealed class VmCredentialAdaptiveCardSession : IExtensionAdaptiveCardSession2, IDisposable
{
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

    private readonly ApplyConfigurationOperation _operation;
    private readonly ManualResetEvent _sessionStatusChangedEvent = new(false);
    private IExtensionAdaptiveCard? _extensionAdaptiveCard;
    private string? _usernameString;
    private SecureString? _passwordString;
    private bool _disposed;

    public event TypedEventHandler<IExtensionAdaptiveCardSession2, ExtensionAdaptiveCardSessionStoppedEventArgs>? Stopped;

    public VmCredentialAdaptiveCardSession(ApplyConfigurationOperation operation)
    {
        _operation = operation;
    }

    void IExtensionAdaptiveCardSession.Dispose()
    {
        ((IDisposable)this).Dispose();
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _extensionAdaptiveCard = extensionUI;
        var operationResult = _extensionAdaptiveCard.Update(GetTemplate(), null, "VmCredential");
        return operationResult;
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(() =>
        {
            ProviderOperationResult operationResult;
            try
            {
                Logging.Logger()?.ReportInfo($"OnAction() called with state:{_extensionAdaptiveCard?.State}");
                Logging.Logger()?.ReportDebug($"action: {action}");

                switch (_extensionAdaptiveCard?.State)
                {
                    case "VmCredential":
                        {
                            Logging.Logger()?.ReportDebug($"inputs: {inputs}");
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
                            Logging.Logger()?.ReportError($"Unexpected state:{_extensionAdaptiveCard?.State}");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Something went wrong", $"Unexpected state:{_extensionAdaptiveCard?.State}");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Logging.Logger()?.ReportError($"Exception in OnAction: {ex}");
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

    private string GetTemplate()
    {
        return Resources.ReplaceIdentifers(_credentialUITemplate, Resources.GetHyperVResourceIdentifiers(), Logging.Logger());
    }

    private static readonly string _credentialUITemplate = @"
{
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""type"": ""AdaptiveCard"",
    ""version"": ""1.5"",
    ""body"": [
        {
            ""type"": ""TextBlock"",
            ""text"": ""%VmCredentialRequest/Title%"",
            ""horizontalAlignment"": ""Center"",
            ""wrap"": true,
            ""style"": ""heading""
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""%VmCredentialRequest/Description1%"",
            ""wrap"": true
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""%VmCredentialRequest/Description2%"",
            ""wrap"": true
        },
        {
            ""type"": ""Input.Text"",
            ""id"": ""UserVal"",
            ""label"": ""%VmCredentialRequest/UsernameLabel%"",
            ""isRequired"": true,
            ""errorMessage"": ""%VmCredentialRequest/UsernameErrorMsg%""
        },
        {
            ""type"": ""Input.Text"",
            ""id"": ""PassVal"",
            ""style"": ""Password"",
            ""label"": ""%VmCredentialRequest/PasswordLabel%""
        }
    ],
    ""actions"": [
        {
            ""type"": ""Action.Execute"",
            ""title"": ""%VmCredentialRequest/OkText%"",
            ""id"": ""okAction"",
            ""data"": {
                ""id"": ""okAction""
            }
        },
        {
            ""type"": ""Action.Execute"",
            ""title"": ""%VmCredentialRequest/CancelText%"",
            ""id"": ""cancelAction""
        }
    ]
}
";
}
