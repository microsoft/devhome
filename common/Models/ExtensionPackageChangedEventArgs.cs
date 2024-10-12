// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.ApplicationModel;

namespace DevHome.Common.Models;

public enum PackageChangedEventKind
{
    Installed,
    Updated,
    UnInstalled,
}

public class ExtensionPackageChangedEventArgs : EventArgs
{
    public Package Package { get; }

    public PackageChangedEventKind ChangedEventKind { get; }

    public ExtensionPackageChangedEventArgs(Package package, PackageChangedEventKind changeKind)
    {
        Package = package;
        ChangedEventKind = changeKind;
    }
}
