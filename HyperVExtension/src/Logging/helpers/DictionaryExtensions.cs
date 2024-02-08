// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.Logging.Helpers;

public static class DictionaryExtensions
{
    public static void DisposeAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

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
