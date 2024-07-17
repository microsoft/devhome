// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Customization.Models;

public class RepositoryInformation
{
    public string RepositoryRootPath { get; set; }

    public string SourceControlProviderCLSID { get; set; }

    public string SourceControlProviderDisplayName { get; set; }

    public RepositoryInformation(string rootpath, string classId, string name)
    {
        RepositoryRootPath = rootpath;
        SourceControlProviderCLSID = classId;
        SourceControlProviderDisplayName = name;
    }
}
