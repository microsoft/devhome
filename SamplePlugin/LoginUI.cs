// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace SamplePlugin;
internal class LoginUI : IPluginAdaptiveCardController
{
    private IPluginAdaptiveCard pluginUI;

    public void Initialize(IPluginAdaptiveCard pluginUI)
    {
        this.pluginUI = pluginUI;

        pluginUI.Update(
            @"
{
  ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
  ""type"": ""AdaptiveCard"",
  ""version"": ""1.4"",
  ""body"": [
    {
      ""type"": ""TextBlock"",
      ""text"": ""This is a plugin Adaptive Card! Click Submit!""
    },
    {
      ""type"": ""Input.Text"",
      ""id"": ""firstName"",
      ""label"": ""What is your first name?""
    },
    {
      ""type"": ""Input.Text"",
      ""id"": ""lastName"",
      ""label"": ""What is your last name?""
    }
  ],
  ""actions"": [
    {
      ""type"": ""Action.Execute"",
      ""title"": ""Action.Execute"",
      ""verb"": ""doStuff"",
      ""data"": {
        ""x"": 13
      }
    }
  ]
}
", null,
            null);
    }

    public void Dispose()
    {
    }

    public void OnAction(string actionVerb, string args)
    {
        pluginUI.Update(
            @"
{
  ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
  ""type"": ""AdaptiveCard"",
  ""version"": ""1.4"",
  ""body"": [
    {
      ""type"": ""TextBlock"",
      ""text"": ""Form **submitted**! Thank you :)""
    }
  ]
}
", null,
            null);
    }
}
