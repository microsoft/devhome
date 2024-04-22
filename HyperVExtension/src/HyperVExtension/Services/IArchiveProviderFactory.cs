// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperVExtension.Models.VirtualMachineCreation;
using Windows.Storage;

namespace HyperVExtension.Services;

public interface IArchiveProviderFactory
{
    public IArchiveProvider CreateArchiveProvider(string extension);
}
