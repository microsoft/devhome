// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using DevHome.Common.Models.ExtensionJsonData;

namespace DevHome.Common.Helpers;

public static class CommonConstants
{
#if CANARY_BUILD
    public const string HyperVExtensionClassId = "6B219EF0-E238-434C-952E-4DF3D452AC83";

    public const string WSLExtensionClassId = "EF2342AC-FF53-433D-9EDE-D395500F3B3E";

    public const string GitExtensionClassId = "A65E46FF-F979-480d-A379-1FDA3EB5F7C5";

    public const string SourceControlServerClassId = "8DDE51FC-3AE8-4880-BD85-CA57DF7E2889";
#elif STABLE_BUILD
    public const string HyperVExtensionClassId = "F8B26528-976A-488C-9B40-7198FB425C9E";

    public const string WSLExtensionClassId = "121253AB-BA5D-4E73-99CF-25A2CB8BF173";

    public const string GitExtensionClassId = "8A962CBD-530D-4195-8FE3-F0DF3FDDF128";

    public const string SourceControlServerClassId = "1212F95B-257E-414e-B44F-F26634BD2627";
#else
    public const string HyperVExtensionClassId = "28DD4098-162D-483C-9ED0-FB3887A22F61";

    public const string WSLExtensionClassId = "7F572DC5-F40E-440F-B660-F579168B69B8";

    public const string GitExtensionClassId = "BDA76685-E749-4f09-8F13-C466D0802DA1";

    public const string SourceControlServerClassId = "40FE4D6E-C9A0-48b4-A83E-AAA1D002C0D5";
#endif

    public const string HyperVWindowsOptionalFeatureName = "Microsoft-Hyper-V";

    public static readonly JsonSerializerOptions ExtensionJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new JsonSourceGenerationContext(),
        TypeInfoResolverChain = { JsonSourceGenerationContext.Default },
    };

    public static readonly string LocalExtensionJsonSchemaRelativeFilePath = @"Assets\Schemas\ExtensionInformation.schema.json";

    public static readonly string LocalExtensionJsonRelativeFilePath = @"Assets\ExtensionInformation.json";
}
