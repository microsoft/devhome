// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;

namespace DevHome.Stub;

[SupportedOSPlatform("Windows10.0.21200.0")]
public class Start
{
    [STAThread]
    public static void Main(string[] args)
    {
        var application = new App();
        if (args.Length != 0)
        {
            application.Properties.Add("protocolArgs", string.Join(string.Empty, args));
        }

        application.InitializeComponent();
        application.Run();
    }
}

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
[SupportedOSPlatform("Windows10.0.21200.0")]
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        FontSizeManager.Register(Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains("Fonts")));
    }
}
