// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using Microsoft.Extensions.Options;

namespace DevHome.SetupFlow.Services;
public class SetupFlowStringResource : StringResource, ISetupFlowStringResource
{
    public SetupFlowStringResource(IOptions<SetupFlowOptions> setupFlowOptions)
        : base(setupFlowOptions.Value.StringResourcePath)
    {
    }
}
