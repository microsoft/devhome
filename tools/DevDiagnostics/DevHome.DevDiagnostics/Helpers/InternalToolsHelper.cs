// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DevHome.DevDiagnostics.Helpers;

internal sealed class InternalToolsHelper
{
    private readonly List<Tool> _allInternalTools;

    public ReadOnlyCollection<Tool> AllInternalTools => _allInternalTools.AsReadOnly();

    public InternalToolsHelper()
    {
        // All internal tools should be in this list
        _allInternalTools = new List<Tool>
        {
            new ClipboardMonitorInternalTool(),
        };
    }
}
