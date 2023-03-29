// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Logging;
public partial class Options : ICloneable
{
    public FailFastSeverityLevel FailFastSeverity { get; set; } = FailFastSeverityLevel.Critical;

    public object Clone() => MemberwiseClone();
}
