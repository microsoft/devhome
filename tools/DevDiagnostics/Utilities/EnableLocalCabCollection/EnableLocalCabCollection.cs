// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.DevDiagnostics.Helpers;

namespace EnableLocalCabCollection;

internal sealed class EnableLocalCabCollection
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Debug.WriteLine("Unexpected command line");
            return;
        }

        if (WERUtils.IsCollectionEnabledForApp(args[0]))
        {
            WERUtils.DisableCollectionForApp(args[0]);
        }
        else
        {
            WERUtils.EnableCollectionForApp(args[0]);
        }
    }
}
