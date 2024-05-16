// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace FileExplorerSourceControlIntegration;

#nullable enable
[ComVisible(true)]
[Guid("40FE4D6E-C9A0-48B4-A83E-AAA1D002C0D5")]
public class SourceControlProvider : Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootSelector
{
    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(SourceControlProvider));

    public SourceControlProvider()
    {
    }

    public Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootPropertyProvider? GetProvider(string rootPath)
    {
        ILocalRepositoryProvider localRepositoryProvider = GetLocalProvider(rootPath);
        GetLocalRepositoryResult result = localRepositoryProvider.GetRepository(rootPath);
        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            log.Information("Could not open local repository.");
            log.Information(result.Result.DisplayMessage);
            return null;
        }

        return new RootFolderPropertyProvider(result.Repository);
    }

    internal ILocalRepositoryProvider GetLocalProvider(string rootPath)
    {
        // TODO: Iterate extensions to find the correct one for this rootPath.
        ILocalRepositoryProvider? provider = null;
        var providerPtr = IntPtr.Zero;
        try
        {
            // "BDA76685-E749-4f09-8F13-C466D0802DA1" is the hardcoded GUID for the git extension. In a future change, the value will no longer be hard-coded and
            // will be discoverable given a root path from a storage location
            var hr = PInvoke.CoCreateInstance(Guid.Parse("BDA76685-E749-4f09-8F13-C466D0802DA1"), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(ILocalRepositoryProvider).GUID, out var extensionObj);
            providerPtr = Marshal.GetIUnknownForObject(extensionObj);
            if (hr < 0)
            {
                Log.Debug("Failure occurred while creating instance of repository provider");
                Marshal.ThrowExceptionForHR(hr);
            }

            provider = MarshalInterface<ILocalRepositoryProvider>.FromAbi(providerPtr);
        }
        finally
        {
            if (providerPtr != IntPtr.Zero)
            {
                Marshal.Release(providerPtr);
            }
        }

        Log.Information("GetLocalProvider succeeded");
        return provider;
    }
}

internal sealed class RootFolderPropertyProvider : Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootPropertyProvider
{
    public RootFolderPropertyProvider(ILocalRepository repository)
    {
        _repository = repository;
    }

    public IPropertySet GetProperties(string[] properties, string relativePath)
    {
        return _repository.GetProperties(properties, relativePath);
    }

    private readonly ILocalRepository _repository;
}
