using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace SampleExtension;
internal class LoginUI : IExtensionAdaptiveCardController
{
    private IExtensionAdaptiveCard extensionUI;

    public void Initialize(IExtensionAdaptiveCard extensionUI)
    {
        this.extensionUI = extensionUI;

        extensionUI.Update(
            @"
{
  ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
  ""type"": ""AdaptiveCard"",
  ""version"": ""1.4"",
  ""body"": [
    {
      ""type"": ""TextBlock"",
      ""text"": ""This is an extension Adaptive Card! Click Submit!""
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
        extensionUI.Update(
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
