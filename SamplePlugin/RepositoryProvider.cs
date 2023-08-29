// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace SampleExtension;
internal class RepositoryProvider : IRepositoryProvider
{
    public string GetDisplayName() => "Repository Provider";

    public IAsyncOperation<IEnumerable<IRepository>> GetRepositoriesAsync(IDeveloperId developerId) => throw new NotImplementedException();
}
