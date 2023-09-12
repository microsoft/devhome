// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);

    Task HandleFileActivationOnLaunched(object activationArgs);
}
