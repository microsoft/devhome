// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Templating;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Services;
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

    public AccountsPageViewModel AccountsPageViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = Application.Current.GetService<SettingsViewModel>();
        AccountsPageViewModel = Application.Current.GetService<AccountsPageViewModel>();
        InitializeComponent();

        var template = new AdaptiveCardTemplate(templateJson);
        var adaptiveCardString = template.Expand(JsonConvert.DeserializeObject<JObject>(dataJson));
        var parseResult = AdaptiveCard.FromJsonString(adaptiveCardString);
        var element = new AdaptiveCardRenderer().RenderAdaptiveCard(parseResult.AdaptiveCard).FrameworkElement;
        ContentArea.Children.Add(element);
    }

    private async void AddDeveloperId_Click(object sender, RoutedEventArgs e)
    {
        if (AccountsPageViewModel.AccountsProviders.Count == 0)
        {
            var confirmLogoutContentDialog = new ContentDialog
            {
                Title = "No Dev Home Plugins found!",
                Content = "Please install a Dev Home Plugin and restart Dev Home to add an account.",
                PrimaryButtonText = "Ok",
                XamlRoot = XamlRoot,
            };

            await confirmLogoutContentDialog.ShowAsync();
            return;
        }

        // TODO: expand this for multiple providers after their buttons are added
        AccountsPageViewModel.AccountsProviders.First().AddAccount();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var confirmLogoutContentDialog = new ContentDialog
        {
            Title = "Are you sure?",
            Content = "Are you sure you want to remove this user account?"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Dev Home will no longer be able to access online resources that use this account.",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        var contentDialogResult = await confirmLogoutContentDialog.ShowAsync();

        if (contentDialogResult.Equals(ContentDialogResult.Secondary))
        {
            return;
        }

        var loginIdToRemove = (sender as Button)?.Tag.ToString();
        if (string.IsNullOrEmpty(loginIdToRemove))
        {
            return;
        }

        AccountsPageViewModel.AccountsProviders.First().RemoveAccount(loginIdToRemove);

        var afterLogoutContentDialog = new ContentDialog
        {
            Title = "Logout Successful",
            Content = loginIdToRemove + " has successfully logged out",
            PrimaryButtonText = "OK",
            XamlRoot = XamlRoot,
        };
        _ = await afterLogoutContentDialog.ShowAsync();
    }
}
