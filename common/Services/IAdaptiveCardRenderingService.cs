// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Common.Services;

public interface IAdaptiveCardRenderingService
{
    public Task<AdaptiveCardRenderer> GetRenderer();

    public event EventHandler RendererUpdated;
}
