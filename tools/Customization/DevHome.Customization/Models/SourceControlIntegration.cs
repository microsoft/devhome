// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using DevHome.Customization.Views;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Win32;
using WinRT;

namespace DevHome.Customization.Models;

public class SourceControlIntegration
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext("SourceContext", nameof(Models.SourceControlIntegration));

    public static bool ValidateSourceControlExtension(string extensionCLSID, string rootPath)
    {
        var providerPtr = IntPtr.Zero;
        try
        {
            var hr = PInvoke.CoCreateInstance(Guid.Parse(extensionCLSID), null, Windows.Win32.System.Com.CLSCTX.CLSCTX_LOCAL_SERVER, typeof(ILocalRepositoryProvider).GUID, out var extensionObj);
            providerPtr = Marshal.GetIUnknownForObject(extensionObj);
            if (hr < 0)
            {
                Log.Error(hr.ToString(), "Failure occurred while creating instance of repository provider");
                return false;
            }

            ILocalRepositoryProvider provider = MarshalInterface<ILocalRepositoryProvider>.FromAbi(providerPtr);
            GetLocalRepositoryResult result = provider.GetRepository(rootPath);

            if (result.Result.Status == ProviderOperationStatus.Failure)
            {
                Log.Error("Could not open local repository.");
                Log.Error(result.Result.DisplayMessage);
                return false;
            }
            else
            {
                Log.Information("Local repository opened successfully.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while validating source control extension.");
            return false;
        }
        finally
        {
            if (providerPtr != IntPtr.Zero)
            {
                Marshal.Release(providerPtr);
            }
        }

        return true;
    }
}
