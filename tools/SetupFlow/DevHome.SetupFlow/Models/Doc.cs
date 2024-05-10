// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.SetupFlow.Models;

public class Doc
{
    public string Name
    {
        get; set;
    }

    public string Codespaces
    {
        get; set;
    }

    public string Prompt
    {
        get; set;
    }

    public string Code
    {
        get; set;
    }

    public string Readme
    {
        get; set;
    }

    public IReadOnlyList<float> Embedding
    {
        get; set;
    }

    public string Language
    {
        get; set;
    }
}
