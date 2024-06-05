// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

public class RepositorySearchInformation
{
    public IEnumerable<IRepository> Repositories { get; set; } = Enumerable.Empty<IRepository>();

    public string SelectionOptionsPlaceHolderText { get; set; } = string.Empty;

    public List<string> SelectionOptions { get; set; } = new List<string>();

    public string SelectionOptionsLabel { get; set; } = string.Empty;
}
