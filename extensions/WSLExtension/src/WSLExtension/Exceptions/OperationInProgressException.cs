// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLExtension.Common;

namespace WSLExtension.Exceptions;

internal sealed class OperationInProgressException : Exception
{
    public OperationInProgressException(IStringResource stringResource)
        : base(stringResource.GetLocalized("OperationInProgressError"))
    {
    }
}
