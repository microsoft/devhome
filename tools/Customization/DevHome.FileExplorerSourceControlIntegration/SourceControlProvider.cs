// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
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
public class SourceControlProvider :
    Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IExtraFolderPropertiesHandler,
    Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootSelector
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
            var repositoryTracker = new RepositoryTracking(null);
            var activationGUID = repositoryTracker.GetSourceControlProviderForRootPath(rootPath);

            var hr = PInvoke.CoCreateInstance(Guid.Parse(activationGUID), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(ILocalRepositoryProvider).GUID, out var extensionObj);
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

    IDictionary<string, object> IExtraFolderPropertiesHandler.GetProperties(string[] propertyStrings, string rootFolderPath, string relativePath)
    {
        var localProvider = GetLocalProvider(rootFolderPath);
        var localProviderResult = localProvider.GetRepository(rootFolderPath);
        if (localProviderResult.Result.Status == ProviderOperationStatus.Failure)
        {
            log.Warning("Could not open local repository.");
            log.Warning(localProviderResult.Result.DisplayMessage);
            throw new ArgumentException(localProviderResult.Result.DisplayMessage);
        }

        return localProviderResult.Repository.GetProperties(propertyStrings, relativePath);
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
