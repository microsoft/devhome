// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Models;

/// <summary>
/// Represents progress of an operation that require transferring bytes from one place to another.
/// </summary>
public class ProgressObject
{
    public long BytesTransferred { get; set; }

    public long TotalBytes { get; set; }

    public uint PercentageComplete => (uint)((BytesTransferred / (double)TotalBytes) * 100);

    public ProgressObject(long bytesTransferred, long totalBytes)
    {
        BytesTransferred = bytesTransferred;
        TotalBytes = totalBytes;
    }
}
