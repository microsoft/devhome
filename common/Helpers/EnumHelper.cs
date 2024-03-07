// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DevHome.Common.Helpers;

/// <summary>
/// Gets a list of supported operations for a given flag based enum variable.
/// </summary>
public static class EnumHelper
{
    public static List<string> SupportedOperationsToString<T>(T operations)
        where T : Enum
    {
        var supportedOperations = new List<string>();

        foreach (T operation in Enum.GetValues(operations.GetType()))
        {
            if (operations.HasFlag(operation))
            {
                supportedOperations.Add(operation.ToString());
            }
        }

        return supportedOperations;
    }
}
