// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Properties;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class PreferencesViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(PreferencesViewModel));

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private ElementTheme _elementTheme;

    public PreferencesViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(CommonHelper.GetLocalizedString("SettingsPageHeader"), typeof(SettingsPageViewModel).FullName!),
            new(CommonHelper.GetLocalizedString("SettingsPreferencesHeader"), typeof(PreferencesViewModel).FullName!),
        };

        ThemeName t = ThemeName.Themes.First(t => t.Name == Settings.Default.CurrentTheme);
        ElementTheme = t.Theme;
    }

    [RelayCommand]
    private void SwitchTheme(ElementTheme elementTheme)
    {
        ElementTheme = elementTheme;
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.SetRequestedTheme(elementTheme);
        Settings.Default.CurrentTheme = elementTheme.ToString();
        Settings.Default.Save();
    }
}
