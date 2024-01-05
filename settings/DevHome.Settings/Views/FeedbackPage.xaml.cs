// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using Microsoft.Management.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.System.Profile;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace DevHome.Settings.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FeedbackPage : Page
{
    private static readonly double ByteSizeGB = 1024 * 1024 * 1024;
    private static string wmiCPUInfo = string.Empty;

    public FeedbackViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get;
    }

    public FeedbackPage()
    {
        ViewModel = Application.Current.GetService<FeedbackViewModel>();
        InitializeComponent();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new Breadcrumb(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new Breadcrumb(stringResource.GetLocalized("Settings_Feedback_Header"), typeof(ExtensionsViewModel).FullName!),
        };
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
    }

    private async void DisplaySuggestFeature(object sender, RoutedEventArgs e)
    {
        var result = await suggestFeatureDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var issueTitle = HttpUtility.UrlEncode(SuggestFeatureTitle.Text);
            var description = HttpUtility.UrlEncode(SuggestFeatureDescription.Text);
            var scenario = HttpUtility.UrlEncode(SuggestFeatureScenario.Text);
            var supportingInfo = HttpUtility.UrlEncode(SuggestFeatureSupportingInfo.Text);
            var gitHubURL = "https://github.com/microsoft/devhome/issues/new?title=" + issueTitle +
                "&labels=Issue-Feature&template=Feature_Request.yml&description=" + description +
                "&scenario=" + scenario +
                "&supportinginfo=" + supportingInfo;

            // Make sure any changes are consistent with the feature request issue template on GitHub
            await Windows.System.Launcher.LaunchUriAsync(new Uri(gitHubURL));
        }
        else
        {
            suggestFeatureDialog.Hide();
        }

        SuggestFeatureTitle.Text = SuggestFeatureDescription.Text = SuggestFeatureScenario.Text = SuggestFeatureSupportingInfo.Text = string.Empty;
    }

    private async void DisplayLocalizationIssueDialog(object sender, RoutedEventArgs e)
    {
        var result = await LocalizationIssueDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var version = HttpUtility.UrlEncode(GetAppVersion());
            var issueTitle = HttpUtility.UrlEncode(LocalizationIssueTitle.Text);
            var languageAffected = HttpUtility.UrlEncode(LocalizationIssueLanguageAffected.Text);
            var gitHubURL = "https://github.com/microsoft/devhome/issues/new?title=" + issueTitle +
                "&labels=Issue-Translation&template=Translation_Issue.yml&version=" + version +
                "&languageaffected=" + languageAffected;

            ReportBugExpectedBehavior.Text = ReportBugActualBehavior.Text = string.Empty;

            // Make sure any changes are consistent with the translation issue template on GitHub
            await Windows.System.Launcher.LaunchUriAsync(new Uri(gitHubURL));
        }
        else
        {
            LocalizationIssueDialog.Hide();
        }

        LocalizationIssueTitle.Text = LocalizationIssueLanguageAffected.Text = string.Empty;
    }

    private async void DisplayReportBugDialog(object sender, RoutedEventArgs e)
    {
        var result = await reportBugDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var version = HttpUtility.UrlEncode(GetAppVersion());
            var windowsversion = HttpUtility.UrlEncode(GetWindowsVersion());
            var issueTitle = HttpUtility.UrlEncode(ReportBugIssueTitle.Text);
            var reproSteps = HttpUtility.UrlEncode(ReportBugReproSteps.Text);
            var expectedBehavior = HttpUtility.UrlEncode(ReportBugExpectedBehavior.Text);
            var actualBehavior = HttpUtility.UrlEncode(ReportBugActualBehavior.Text);

            var sysInfo = string.Empty;
            if (ReportBugIncludeSystemInfo.IsChecked.GetValueOrDefault())
            {
                sysInfo = HttpUtility.UrlEncode(wmiCPUInfo + "\n" + GetPhysicalMemory() + "\n" + GetProcessorArchitecture());
            }

            var extensionsInfo = string.Empty;
            if (ReportBugIncludeExtensions.IsChecked.GetValueOrDefault())
            {
                extensionsInfo = HttpUtility.UrlEncode(GetExtensions() + "\n" + GetWidgetService());
            }

            var otherSoftwareText = "OS Build Version: " + GetOSVersion() + "\n.NET Version: " + GetDotNetVersion();
            var otherSoftware = HttpUtility.UrlEncode(otherSoftwareText);

            var gitHubURL = "https://github.com/microsoft/devhome/issues/new?title=" + issueTitle +
                "&labels=Issue-Bug&template=Bug_Report.yml&version=" + version +
                "&windowsversion=" + windowsversion +
                "&repro=" + reproSteps +
                "&expectedbehavior=" + expectedBehavior +
                "&actualbehavior=" + actualBehavior +
                "&includedsysinfo=" + sysInfo +
                "&includedextensionsinfo=" + extensionsInfo +
                "&othersoftware=" + otherSoftware;

            // Make sure any changes are consistent with the report bug issue template on GitHub
            await Windows.System.Launcher.LaunchUriAsync(new Uri(gitHubURL));
        }
        else
        {
            reportBugDialog.Hide();
        }

        ReportBugIncludeSystemInfo.IsChecked = ReportBugIncludeExtensions.IsChecked = ReportBugIncludeExperimentInfo.IsChecked = true;
        ReportBugSysInfoExpander.IsExpanded = ReportBugExtensionsExpander.IsExpanded = ReportBugExperimentInfoExpander.IsExpanded = false;
        ReportBugIssueTitle.Text = ReportBugReproSteps.Text = ReportBugExpectedBehavior.Text = ReportBugActualBehavior.Text = string.Empty;
    }

    private async void DisplayDocumentationIssueDialog(object sender, RoutedEventArgs e)
    {
        var result = await DocumentationIssueDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var issueTitle = HttpUtility.UrlEncode(DocumentationIssueTitle.Text);
            var issueDescription = HttpUtility.UrlEncode(DocumentationIssueDescription.Text);
            var gitHubURL = "https://github.com/microsoft/devhome/issues/new?title=" + issueTitle +
                "&labels=Issue-Docs&projects=&template=Documentation_Issue.yml&description=" + issueDescription;

            // Make sure any changes are consistent with the documentation issue template on GitHub
            await Windows.System.Launcher.LaunchUriAsync(new Uri(gitHubURL));
        }
        else
        {
            DocumentationIssueDialog.Hide();
        }

        DocumentationIssueTitle.Text = DocumentationIssueDescription.Text = string.Empty;
    }

    private void ShowSysInfoExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        PhysicalMemory.Text = GetPhysicalMemory();
        ProcessorArchitecture.Text = GetProcessorArchitecture();
        CpuID.Text = wmiCPUInfo;
    }

    private void ShowExtensionsInfoExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        ReportBugIncludeExtensionsList.Text = GetExtensions();
        WidgetServiceInfo.Text = GetWidgetService();
    }

    private async void Reload()
    {
        wmiCPUInfo = await GetCPUAsync();
    }

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Reload();
    }

    private string GetWindowsVersion()
    {
        var attrNames = new List<string> { "OSVersion" };
        var attrData = AnalyticsInfo.GetSystemPropertiesAsync(attrNames).AsTask().GetAwaiter().GetResult();
        var windowsVersion = string.Empty;
        if (attrData.ContainsKey("OSVersion"))
        {
            windowsVersion = attrData["OSVersion"];
        }

        return windowsVersion.ToString();
    }

    private string GetAppVersion()
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var appInfoService = Application.Current.GetService<IAppInfoService>();
        var version = appInfoService.GetAppVersion();

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    private static async Task<string> GetCPUAsync()
    {
        try
        {
            using var session = CimSession.Create(null);

            var name = (string)await Task.Run(() => session.QueryInstances("root\\CIMV2", "WQL", "SELECT * from Win32_Processor").First().CimInstanceProperties["Name"].Value);
            return "CPU: " + name;
        }
        catch (Exception)
        {
        }

        return string.Empty;
    }

    private string GetDotNetVersion()
    {
        var dotNetVersion = RuntimeInformation.FrameworkDescription;
        return dotNetVersion.ToString();
    }

    private string GetOSVersion()
    {
        var attrNames = new List<string> { "OSVersionFull" };
        var attrData = AnalyticsInfo.GetSystemPropertiesAsync(attrNames).AsTask().GetAwaiter().GetResult();
        var osVersion = string.Empty;
        if (attrData.ContainsKey("OSVersionFull"))
        {
            osVersion = attrData["OSVersionFull"];
        }

        return osVersion.ToString();
    }

    private string GetPhysicalMemory()
    {
        var cultures = new CultureInfo("en-US");

        MEMORYSTATUSEX memStatus = default;
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        PInvoke.GlobalMemoryStatusEx(out memStatus);

        var availMemKbToGb = Math.Round(memStatus.ullAvailPhys / ByteSizeGB, 2);
        var totalMemKbToGb = Math.Round(memStatus.ullTotalPhys / ByteSizeGB, 2);

        var stringResource = new StringResource("DevHome.Settings/Resources");
        return stringResource.GetLocalized("Settings_Feedback_PhysicalMemory") + ": " + totalMemKbToGb.ToString(cultures) + "GB (" + availMemKbToGb.ToString(cultures) + "GB free)";
    }

    private string GetProcessorArchitecture()
    {
        SYSTEM_INFO sysInfo;
        PInvoke.GetSystemInfo(out sysInfo);
        var stringResource = new StringResource("DevHome.Settings/Resources");
        return stringResource.GetLocalized("Settings_Feedback_ProcessorArchitecture") + ": " + DetermineArchitecture((int)sysInfo.Anonymous.Anonymous.wProcessorArchitecture);
    }

    private string DetermineArchitecture(int value)
    {
        var arch = "Unknown architecture";
        switch (value)
        {
            case 9:
                arch = "x64";
                break;
            case 5:
                arch = "ARM";
                break;
            case 12:
                arch = "ARMx64";
                break;
            case 6:
                arch = "Intel Itanium-based";
                break;
            case 0:
                arch = "x86";
                break;
        }

        return arch;
    }

    private string GetExtensions()
    {
        var extensionService = Application.Current.GetService<IExtensionService>();
        var extensions = extensionService.GetInstalledExtensionsAsync(true).Result;
        var stringResource = new StringResource("DevHome.Settings/Resources");
        var extensionsStr = stringResource.GetLocalized("Settings_Feedback_Extensions") + ": \n";
        foreach (var extension in extensions)
        {
            extensionsStr += extension.PackageFullName + "\n";
        }

        return extensionsStr;
    }

    private string GetWidgetService()
    {
        var stringResource = new StringResource("DevHome.Settings/Resources");
        var widgetServiceString = stringResource.GetLocalized("Settings_Feedback_WidgetService") + ": \n";
        var packageDeploymentService = Application.Current.GetService<IPackageDeploymentService>();

        // Only one package is expected in total from these two queries, but print anything just in case.
        const string webExperienceFamilyName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";
        var webPackages = packageDeploymentService.FindPackagesForCurrentUser(webExperienceFamilyName);
        foreach (var package in webPackages)
        {
            widgetServiceString += package.Id.FullName + "\n";
        }

        const string widgetServiceFamilyName = "Microsoft.WidgetsPlatformRuntime_8wekyb3d8bbwe";
        var widgetPackages = packageDeploymentService.FindPackagesForCurrentUser(widgetServiceFamilyName);
        foreach (var package in widgetPackages)
        {
            widgetServiceString += package.Id.FullName + "\n";
        }

        return widgetServiceString;
    }

    private async void BuildExtensionButtonClicked(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new ("https://go.microsoft.com/fwlink/?linkid=2234795"));
    }

    private async void ReportSecurityButtonClicked(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new ("https://github.com/microsoft/devhome/security/policy"));
    }
}
