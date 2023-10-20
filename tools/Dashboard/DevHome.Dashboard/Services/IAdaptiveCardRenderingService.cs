// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Dashboard.Services;

internal interface IAdaptiveCardRenderingService
{
    public Task<AdaptiveCardRenderer> GetRenderer();

    public Task UpdateHostConfig();
}
