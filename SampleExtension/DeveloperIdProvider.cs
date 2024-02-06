// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace SampleExtension;

internal sealed class DeveloperIdProvider : IDeveloperIdProvider
{
    public string DisplayName => $"Sample {nameof(DeveloperIdProvider)}";

#pragma warning disable CS0067 // The event 'DeveloperIdProvider.Changed' is never used
    public event TypedEventHandler<IDeveloperIdProvider, IDeveloperId> Changed;
#pragma warning restore CS0067 // The event 'DeveloperIdProvider.Changed' is never used

    public DeveloperIdsResult GetLoggedInDeveloperIds() => throw new NotImplementedException();

    public ProviderOperationResult LogoutDeveloperId(IDeveloperId developerId) => throw new NotImplementedException();

    public AuthenticationState GetDeveloperIdState(IDeveloperId developerId) => throw new NotImplementedException();

    public AuthenticationExperienceKind GetAuthenticationExperienceKind() => throw new NotImplementedException();

    public IAsyncOperation<DeveloperIdResult> ShowLogonSession(WindowId windowHandle) => throw new NotImplementedException();

    public AdaptiveCardSessionResult GetLoginAdaptiveCardSession() => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
