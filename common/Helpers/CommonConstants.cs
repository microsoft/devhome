// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Helpers;

public static class CommonConstants
{
#if CANARY_BUILD
    public const string HyperVExtensionClassId = "6B219EF0-E238-434C-952E-4DF3D452AC83";

    public const string WSLExtensionClassId = "EF2342AC-FF53-433D-9EDE-D395500F3B3E";
#elif STABLE_BUILD
    public const string HyperVExtensionClassId = "F8B26528-976A-488C-9B40-7198FB425C9E";

    public const string WSLExtensionClassId = "121253AB-BA5D-4E73-99CF-25A2CB8BF173";
#else
    public const string HyperVExtensionClassId = "28DD4098-162D-483C-9ED0-FB3887A22F61";

    public const string WSLExtensionClassId = "7F572DC5-F40E-440F-B660-F579168B69B8";
#endif

    public const string HyperVWindowsOptionalFeatureName = "Microsoft-Hyper-V";
}
