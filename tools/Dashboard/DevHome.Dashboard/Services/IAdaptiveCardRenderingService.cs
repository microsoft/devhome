// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Dashboard.Services;

public interface IAdaptiveCardRenderingService
{
    public Task<AdaptiveCardRenderer> GetRenderer();

    public Task UpdateHostConfig();
}
