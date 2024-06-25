// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace WSLExtension.Models;

public interface IDistroKind : IComparable<IDistroKind>
{
    string Name { get; set; }

    string Logo { get; set; }

    List<string> IdLike { get; set; }
}
