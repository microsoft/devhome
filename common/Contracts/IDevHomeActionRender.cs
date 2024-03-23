// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Renderers;

namespace DevHome.Common.Contracts;

public interface IDevHomeActionRender : IAdaptiveElementRenderer
{
    public void InitiateAction(ActionButtonToInvoke buttonToInvoke);
}
