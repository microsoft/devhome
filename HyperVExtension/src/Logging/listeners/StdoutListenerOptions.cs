// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging;

public partial class Options
{
    public bool LogStdoutEnabled { get; set; } = true;

    public SeverityLevel LogStdoutFilter { get; set; } = SeverityLevel.Info;
}
