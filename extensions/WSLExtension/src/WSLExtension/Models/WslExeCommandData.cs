// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Models;

public enum WslCommandDataKind
{
    Output,
    Error,
}

public class WslExeCommandData
{
    public string Data { get; } = string.Empty;

    public WslCommandDataKind DataKind { get; }

    public WslExeCommandData(string data, WslCommandDataKind dataKind)
    {
        Data = data;
        DataKind = dataKind;
    }
}
