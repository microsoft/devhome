// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Helpers;

public static class AdaptiveCardRendererHelper
{
    public static AdaptiveCardRenderer GetLoginUIRenderer()
    {
        var adaptiveCardRenderer = new AdaptiveCardRenderer();

        var hostConfigJSON = @"
{
	""supportsInteractivity"": true,
	""actions"": {
		""actionsOrientation"": ""vertical"",
		""actionAlignment"": ""stretch""
	}
}
";
        var hostConfig = AdaptiveHostConfig.FromJsonString(hostConfigJSON);

        adaptiveCardRenderer.HostConfig = hostConfig.HostConfig;

        return adaptiveCardRenderer;
    }
}
