// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace TestDumpAnalyzer;

internal sealed class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("args are: " + args.Length + " " + string.Join(" ", args));

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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.ReadLine();
        }

        return 0;
    }
}
