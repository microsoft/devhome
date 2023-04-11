// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System.Diagnostics;

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

    private void OpenLink(object obj)
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
