// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Logging;

namespace DevHome.Common.Helpers;

public class Log
{
    public static Logger? Logger { get; } = new ComponentLogger("DevHome.Common").Logger;
}
