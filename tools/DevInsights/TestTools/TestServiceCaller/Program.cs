// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.Service;
using WinRT;

namespace TestServiceCaller;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var devHomeService = GetDevHomeService();
            var number = devHomeService.GetNumber();
            Console.WriteLine("Number = " + number);
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"Error: {e.Message}");
        }
    }

    public static IDevHomeService GetDevHomeService()
    {
        var serverClass = new DevHomeServer();
        var serverPtr = Marshal.GetIUnknownForObject(serverClass);
        var server = MarshalInterface<IDevHomeService>.FromAbi(serverPtr);

        return server;
    }

    [ComImport]
#if CANARY_BUILD
    [Guid("0A920C6E-2569-44D1-A6E4-CE9FA44CD2A7")]
#elif STABLE_BUILD
    [Guid("E8D40232-20A1-4F3B-9C0C-AAA6538698C6")]
#else
    [Guid("1F98F450-C163-4A99-B257-E1E6CB3E1C57")]
#endif
    public class DevHomeServer;
}
