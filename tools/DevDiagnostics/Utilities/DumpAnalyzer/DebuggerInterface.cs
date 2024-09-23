// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Interop;

namespace ConsoleDumpAnalyzer;

internal sealed class DebuggerInterface
{
    public static IDebugClient4 GetDebugger()
    {
        Guid g = typeof(IDebugClient4).GUID;

        DebugCreate(ref g, out object debugger);

        return (IDebugClient4)debugger;
    }

    [DllImport("dbgeng.dll")]
    public static extern void DebugCreate(ref Guid interfaceGuid, [MarshalAs(UnmanagedType.IUnknown)] out object debugger);
}
