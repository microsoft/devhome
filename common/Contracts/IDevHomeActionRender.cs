// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Common.Contracts;

public interface IDevHomeActionRender : IAdaptiveElementRenderer
{
    public bool TryValidateAndInitiateAction(string buttonId, AdaptiveInputs userInputs);

    public void InitiateAction(string buttonId);
}
