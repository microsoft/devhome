// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging;

public partial class Options : ICloneable
{
    public FailFastSeverityLevel FailFastSeverity { get; set; } = FailFastSeverityLevel.Critical;

    public object Clone() => MemberwiseClone();
}
