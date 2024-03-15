// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging.Helpers;

public static class DictionaryExtensions
{
    public static void DisposeAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        foreach (var kv in dictionary)
        {
            if (kv.Key is IDisposable keyDisposable)
            {
                keyDisposable.Dispose();
            }

            if (kv.Value is IDisposable valDisposable)
            {
                valDisposable.Dispose();
            }
        }
    }
}
