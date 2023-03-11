// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Templating;
using DevHome.Common.Extensions;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{

    private string templateJson = @"

{
    ""type"": ""AdaptiveCard"",
    ""version"": ""1.0"",
    ""body"": [
        {
            ""type"": ""ColumnSet"",
            ""style"": ""accent"",
            ""bleed"": true,
            ""columns"": [
                {
                    ""type"": ""Column"",
                    ""width"": ""auto"",
                    ""items"": [
                        {
                            ""type"": ""Image"",
                            ""url"": ""${photo}"",
                            ""altText"": ""Profile picture"",
                            ""size"": ""Small"",
                            ""style"": ""Person""
                        }
                    ]
                },
                {
                    ""type"": ""Column"",
                    ""width"": ""stretch"",
                    ""items"": [
                        {
                            ""type"": ""TextBlock"",
                            ""text"": ""Hi ${name}!"",
                            ""size"": ""Medium""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": ""Here's a bit about your org..."",
                            ""spacing"": ""None""
                        }
                    ]
                }
            ]
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""Your manager is: **${manager.name}**""
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""Your peers are:""
        },
        {
            ""type"": ""FactSet"",
            ""facts"": [
                {
                    ""$data"": ""${peers}"",
                    ""title"": ""${name}"",
                    ""value"": ""${title}""
                }
            ]
        }
    ]
}";

    private string dataJson = @"
{
    ""name"": ""Matt"",
    ""photo"": ""https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg"",
    ""manager"": {
        ""name"": ""Thomas"",
        ""title"": ""PM Lead""
    },
    ""peers"": [
        {
            ""name"": ""Lei"",
            ""title"": ""Sr Program Manager""
        },
        {
            ""name"": ""Andrew"",
            ""title"": ""Program Manager II""
        },
        {
            ""name"": ""Mary Anne"",
            ""title"": ""Program Manager""
        }
    ]
}
";

    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = Application.Current.GetService<SettingsViewModel>();
        InitializeComponent();

        var template = new AdaptiveCardTemplate(templateJson);
        var adaptiveCardString = template.Expand(JsonConvert.DeserializeObject<JObject>(dataJson));
        var parseResult = AdaptiveCard.FromJsonString(adaptiveCardString);
        var element = new AdaptiveCardRenderer().RenderAdaptiveCard(parseResult.AdaptiveCard).FrameworkElement;
        ContentArea.Children.Add(element);
    }
}
