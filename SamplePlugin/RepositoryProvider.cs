// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace SamplePlugin;

[ComVisible(true)]
[Guid("E99E3C0B-77F2-403E-A3B7-38EC9E410E4D")]
[ComDefaultInterface(typeof(IRepositoryProvider))]
internal class RepositoryProvider : IRepositoryProvider
{
    public string DisplayName => throw new NotImplementedException();

    public IAsyncOperation<IEnumerable<IRepository>> GetRepositoriesAsync(IDeveloperId developerId) => throw new NotImplementedException();

    public IAsyncOperation<IRepository> ParseRepositoryFromUrlAsync(Uri uri) => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
