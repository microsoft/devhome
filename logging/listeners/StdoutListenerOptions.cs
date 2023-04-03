// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Logging;
public partial class Options
{
    public bool LogStdoutEnabled { get; set; } = true;

    public SeverityLevel LogStdoutFilter { get; set; } = SeverityLevel.Info;
}
