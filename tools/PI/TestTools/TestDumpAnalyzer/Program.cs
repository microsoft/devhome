// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace TestDumpAnalyzer;

internal sealed class Program
{
    public static int Main(string[] args)
    {
        // Second param should be the dump file
        FileInfo dumpFile = new(args[1]);

        if (!dumpFile.Exists)
        {
            Console.WriteLine("Dump file does not exist: " + dumpFile.FullName);
            return -1;
        }

        Console.WriteLine("Dump file: " + dumpFile.FullName);
        Console.WriteLine("Dump file length: " + dumpFile.Length);
        Console.WriteLine("Dump file creation time: " + dumpFile.CreationTime);

        return 0;
    }
}
