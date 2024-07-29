// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.PI.Helpers;

internal sealed class InternalToolsHelper
{
    public static readonly InternalToolsHelper Instance = new();

    private readonly List<Tool> _allInternalTools;

    public ReadOnlyCollection<Tool> AllInternalTools => _allInternalTools.AsReadOnly();

    private InternalToolsHelper()
    {
        _allInternalTools = new List<Tool>
        {
            new ClipboardMonitorInternalTool(),
        };
    }
}
