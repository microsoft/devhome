// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace DevHome.Stub.ViewModel;

public class ErrorViewModel : ViewModelBase
{
    public ErrorViewModel(string prefix, string suffix, string linkText, string uri)
    {
        Prefix = prefix;
        Suffix = suffix;
        LinkText = linkText;
        Uri = uri;

        OpenLinkCommand = new RelayCommand(OpenLink);
    }

    public RelayCommand OpenLinkCommand
    {
        get;
    }

    public string Prefix
    {
        get;
    }

    public string Suffix
    {
        get;
    }

    public string LinkText
    {
        get;
    }

    public string Uri
    {
        get;
    }

    private void OpenLink()
    {
        try
        {
            Process.Start(Uri);
        }
        catch
        {
        }
    }
}
