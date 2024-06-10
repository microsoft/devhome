// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Helpers;

public static class CommonConstants
{
    public static readonly Dictionary<string, string> ExtensionToFeatureNameMap = new()
    {
        { HyperVExtensionClassId, HyperVWindowsOptionalFeatureName },
        { WindowsSandBoxExtensionClassId, WindowsSandboxOptionalFeatureName },
    };

    public const string HyperVExtensionClassId = "F8B26528-976A-488C-9B40-7198FB425C9E";

    public const string WindowsSandBoxExtensionClassId = "6A52115B-083C-4FB1-85F4-BBE23289220E";

    public const string HyperVWindowsOptionalFeatureName = "Microsoft-Hyper-V";

    public const string WindowsSandboxOptionalFeatureName = "Containers-DisposableClientVM";
}
