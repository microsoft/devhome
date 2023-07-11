// ==++==
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// ==--==
/*============================================================
**
** Class:  StringBuilderCache
**
** Purpose: provide a cached reusable instance of stringbuilder
**          per thread  it's an optimisation that reduces the
**          number of instances constructed and collected.
**
**  Acquire - is used to get a string builder to use of a
**            particular size.  It can be called any number of
**            times, if a stringbuilder is in the cache then
**            it will be returned and the cache emptied.
**            subsequent calls will return a new stringbuilder.
**
**            A StringBuilder instance is cached in
**            Thread Local Storage and so there is one per thread
**
**  Release - Place the specified builder in the cache if it is
**            not too big.
**            The stringbuilder should not be used after it has
**            been released.
**            Unbalanced Releases are perfectly acceptable.  It
**            will merely cause the runtime to create a new
**            stringbuilder next time Acquire is called.
**
**  GetStringAndRelease
**          - ToString() the stringbuilder, Release it to the
**            cache and return the resulting string
**
===========================================================*/
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1310 // Field names should not contain underscore

using System.Threading;

namespace System.Text;

internal static class StringBuilderCache
{
    // The value 360 was chosen in discussion with performance experts as a compromise between using
    // as little memory (per thread) as possible and still covering a large part of short-lived
    // StringBuilder creations on the startup path of VS designers.
    private const int MAX_BUILDER_SIZE = 360;

    [ThreadStatic]

    private static StringBuilder? CachedInstance;

    public static StringBuilder Acquire(int capacity = /*StringBuilder.DefaultCapacity*/ 16)
    {
        if (capacity <= MAX_BUILDER_SIZE)
        {
            StringBuilder? sb = CachedInstance;
            if (sb != null)
            {
                // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                // when the requested size is larger than the current capacity
                if (capacity <= sb.Capacity)
                {
                    StringBuilderCache.CachedInstance = null;
                    sb.Clear();
                    return sb;
                }
            }
        }

        return new StringBuilder(capacity);
    }

    public static void Release(StringBuilder sb)
    {
        if (sb.Capacity <= MAX_BUILDER_SIZE)
        {
            StringBuilderCache.CachedInstance = sb;
        }
    }

    public static string GetStringAndRelease(StringBuilder sb)
    {
        var result = sb.ToString();
        Release(sb);
        return result;
    }

    public static string GetString(StringBuilder sb)
    {
        return sb.ToString();
    }
}
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning restore SA1306 // Field names should begin with lower-case letter
