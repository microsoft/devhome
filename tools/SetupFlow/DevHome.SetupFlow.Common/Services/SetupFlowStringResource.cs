// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using Microsoft.Extensions.Options;

namespace DevHome.SetupFlow.Common.Services;
public class SetupFlowStringResource : StringResource
{
    public SetupFlowStringResource(IOptions<SetupFlowOptions> setupFlowOptions)
        : base(setupFlowOptions.Value.StringResourcePath)
    {
    }
}
