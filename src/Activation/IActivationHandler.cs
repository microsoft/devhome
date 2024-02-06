// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);

    Task HandleAsync(object args);
}
