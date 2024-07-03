// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using DevHome.Customization.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Win32;
using WinRT;

namespace DevHome.Customization.Models;

public class SourceControlIntegration
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext("SourceContext", nameof(Models.SourceControlIntegration));

    public static SourceControlValidationResult ValidateSourceControlExtension(string extensionCLSID, string rootPath)
    {
        var providerPtr = IntPtr.Zero;
        try
        {
            Log.Information("Validating source control extension with arguments: extensionCLSID = {extensionCLSID}, rootPath = {rootPath}", extensionCLSID, rootPath);

            var hr = PInvoke.CoCreateInstance(Guid.Parse(extensionCLSID), null, Windows.Win32.System.Com.CLSCTX.CLSCTX_LOCAL_SERVER, typeof(ILocalRepositoryProvider).GUID, out var extensionObj);
            providerPtr = Marshal.GetIUnknownForObject(extensionObj);
            if (hr < 0)
            {
                Log.Error(hr.ToString(), "Failure occurred while creating instance of repository provider");
                return new SourceControlValidationResult(ResultType.Failure, ErrorType.RepositoryProvderCreationFailed, null);
            }

            ILocalRepositoryProvider provider = MarshalInterface<ILocalRepositoryProvider>.FromAbi(providerPtr);
            GetLocalRepositoryResult result = provider.GetRepository(rootPath);

            if (result.Result.Status == ProviderOperationStatus.Failure)
            {
                Log.Error("Could not open local repository.");
                Log.Error(result.Result.DisplayMessage);
                return new SourceControlValidationResult(ResultType.Failure, ErrorType.OpenRepositoryFailed, null);
            }
            else
            {
                Log.Information("Local repository opened successfully.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while validating source control extension.");
            return new SourceControlValidationResult(ResultType.Failure, ErrorType.SourceControlExtensionValidationFailed, ex);
        }
        finally
        {
            if (providerPtr != IntPtr.Zero)
            {
                Marshal.Release(providerPtr);
            }
        }

        return new SourceControlValidationResult();
    }
}
