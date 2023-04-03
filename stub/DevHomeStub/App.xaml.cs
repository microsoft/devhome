// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Linq;
using System.Windows;
using Microsoft.Flow.RPA.Desktop.Shared.UI;

namespace Microsoft.DevHome.Stub
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            FontSizeManager.Register(Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains("Fonts")));
        }
    }
}
