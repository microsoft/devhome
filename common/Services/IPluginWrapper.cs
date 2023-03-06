// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.DevHome.SDK;
using WinRT;

namespace DevHome.Common.Services;
public interface IPluginWrapper
{
    string Name
    {
        get;
    }

    bool IsRunning();

    Task StartPlugin();

    void Kill();

    IPlugin? GetPluginObject();

    void AddProviderType(ProviderType providerType);

    bool HasProviderType(ProviderType providerType);
}
