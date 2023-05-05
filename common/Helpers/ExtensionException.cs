// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Helpers;

public class ExtensionException : Exception
{
    public ExtensionException()
        : base()
    {
    }

    public ExtensionException(string message)
        : base(message)
    {
    }

    public ExtensionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
