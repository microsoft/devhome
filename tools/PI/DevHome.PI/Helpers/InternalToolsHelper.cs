// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DevHome.PI.Helpers;

internal sealed class InternalToolsHelper
{
    public static readonly InternalToolsHelper Instance = new();

    private readonly List<Tool> _allInternalTools;

    public ReadOnlyCollection<Tool> AllInternalTools => _allInternalTools.AsReadOnly();

    private InternalToolsHelper()
    {
        // All internal tools should be in this list
        _allInternalTools = new List<Tool>
        {
            new ClipboardMonitorInternalTool(),
        };
    }
}
