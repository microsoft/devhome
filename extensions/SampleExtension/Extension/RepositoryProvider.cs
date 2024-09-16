// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace SampleExtension;

internal sealed class RepositoryProvider : IRepositoryProvider
{
    public string DisplayName => $"Sample {nameof(RepositoryProvider)}";

    public IRandomAccessStreamReference Icon => throw new NotImplementedException();

    public IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination) => throw new NotImplementedException();

    public IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination, IDeveloperId developerId) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();

    public IAsyncOperation<RepositoryResult> GetRepositoryFromUriAsync(Uri uri) => throw new NotImplementedException();

    public IAsyncOperation<RepositoryResult> GetRepositoryFromUriAsync(Uri uri, IDeveloperId developerId) => throw new NotImplementedException();

    public IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri) => throw new NotImplementedException();

    public IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri, IDeveloperId developerId) => throw new NotImplementedException();

    public IAsyncOperation<RepositoriesResult> GetRepositoriesAsync(IDeveloperId developerId) => throw new NotImplementedException();
}
