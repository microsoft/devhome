// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);

    Task HandleFileActivationOnLaunched(object activationArgs);
}
