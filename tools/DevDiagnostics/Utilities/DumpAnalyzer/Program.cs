// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Diagnostics.Runtime.Interop;

namespace ConsoleDumpAnalyzer;

internal sealed class Program
{
    public static int Main(string[] args)
    {
        DebuggerCallbacks callbacks = new DebuggerCallbacks();
        bool success = false;

        try
        {
            IDebugClient4 debugger = DebuggerInterface.GetDebugger();
            IDebugSymbols3 symbols = (IDebugSymbols3)debugger;

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: <dumpfile> <outputfile>");
                return -2;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Dump file does not exist.");
                return -3;
            }

            debugger.SetOutputMask(DEBUG_OUTPUT.NORMAL | DEBUG_OUTPUT.ERROR | DEBUG_OUTPUT.WARNING | DEBUG_OUTPUT.SYMBOLS);
            debugger.SetOutputCallbacks(callbacks);

            symbols.SetSymbolOptions(SYMOPT.CASE_INSENSITIVE
                                       | SYMOPT.UNDNAME
                                       | SYMOPT.OMAP_FIND_NEAREST
                                       | SYMOPT.DEFERRED_LOADS
                                       | SYMOPT.AUTO_PUBLICS
                                       | SYMOPT.NO_IMAGE_SEARCH
                                       | SYMOPT.FAIL_CRITICAL_ERRORS
                                       | SYMOPT.NO_PROMPTS
                                       | (SYMOPT)0x20000000 /*SYMOPT.DISABLE_SYMSRV_TIMEOUT*/);

            symbols.SetSymbolPathWide(@"srv*https://msdl.microsoft.com/download/symbols");

            debugger.OpenDumpFileWide(args[0], 0);

            IDebugControl4 debugControl = (IDebugControl4)debugger;

            debugControl.WaitForEvent(DEBUG_WAIT.DEFAULT, unchecked((uint)-1));
            debugControl.AddExtensionWide(@"winext\ext.dll", 0, out ulong handle);

            debugControl.GetExtensionFunctionWide(handle, "GetDebugFailureAnalysis", out IntPtr function);
            EXT_GET_DEBUG_FAILURE_ANALYSIS analysisFunction = Marshal.GetDelegateForFunctionPointer<EXT_GET_DEBUG_FAILURE_ANALYSIS>(function);

            // Note, this function will hang if a debugger is attached (something to do with CLR hosting). A native debugger will work fine,
            // so if you need to debug into the !analyze code, remove the below line and just use native debugging
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Debugger attached, no further analysis will be done.");
                return -1;
            }

            var result = analysisFunction(debugger, 0, typeof(IDebugFailureAnalysis2).GUID, out IDebugFailureAnalysis analysisObject);

            if (analysisObject is null)
            {
                return result != 0 ? result : -1;
            }

            IDebugFailureAnalysis2 analysis = (IDebugFailureAnalysis2)analysisObject;

            analysis.GetAnalysisXml(out IXMLDOMElement xmlElement);

            IXMLDOMNode xmlDOMNode = (IXMLDOMNode)xmlElement;

            xmlDOMNode.Get_xml(out string xmlString);

            Console.WriteLine(xmlString);

            File.WriteAllText(args[1], xmlString);

            success = true;
            return 0;
        }
        finally
        {
            // Write out the log to disk if there was an error
            if (!success)
            {
                if (args.Length == 2)
                {
                    File.WriteAllText(args[1] + ".err", callbacks.Log);
                }
            }
        }
    }
}

public class DebuggerCallbacks : IDebugOutputCallbacks
{
    public string Log { get; private set; } = string.Empty;

    public int Output(DEBUG_OUTPUT Mask, string Text)
    {
        Log += Text;
        return 0;
    }
}

public delegate int EXT_GET_DEBUG_FAILURE_ANALYSIS(IDebugClient4 client, ulong flags, Guid classID, out IDebugFailureAnalysis analysis);

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00020400-0000-0000-C000-000000000046")]
public interface IDispatch
{
    int GetTypeInfoCount();

    [return: MarshalAs(UnmanagedType.Interface)]
    ITypeInfo GetTypeInfo([In, MarshalAs(UnmanagedType.U4)] int iTInfo, [In, MarshalAs(UnmanagedType.U4)] int lcid);

    void GetIDsOfNames([In] ref Guid riid, [In, MarshalAs(UnmanagedType.LPArray)] string[] rgszNames, [In, MarshalAs(UnmanagedType.U4)] int cNames, [In, MarshalAs(UnmanagedType.U4)] int lcid, [Out, MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("ed0de363-451f-4943-820c-62dccdfa7e6d")]
public interface IDebugFailureAnalysis
{
    [PreserveSig]
    ulong GetFailureClass();

    [PreserveSig]
    int GetFailureType();

    [PreserveSig]
    ulong GetFailureCode();

    [PreserveSig]
    int Get_tag(int tag);

    [PreserveSig]
    int GetNext(IntPtr entry, int tag, int tagmask);

    [PreserveSig]
    int GetString(int tag, IntPtr str, int maxsize);

    [PreserveSig]
    int GetBuffer(int tag, IntPtr buf, int size);

    [PreserveSig]
    int GetUlong(int tag, out ulong value);

    [PreserveSig]
    int GetUlong64(int tag, out ulong value);

    [PreserveSig]
    int NextEntry(IntPtr entry);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("ea15c288-8226-4b70-acf6-0be6b189e3ad")]
public interface IDebugFailureAnalysis2
{
    [PreserveSig]
    ulong GetFailureClass();

    [PreserveSig]
    int GetFailureType();

    [PreserveSig]
    ulong GetFailureCode();

    [PreserveSig]
    int Get_tag(int tag);

    [PreserveSig]
    int GetNext(IntPtr entry, int tag, int tagmask);

    [PreserveSig]
    int GetString(int tag, IntPtr str, int maxsize);

    [PreserveSig]
    int GetBuffer(int tag, IntPtr buf, int size);

    [PreserveSig]
    int GetUlong(int tag, out ulong value);

    [PreserveSig]
    int GetUlong64(int tag, out ulong value);

    [PreserveSig]
    int NextEntry(IntPtr entry);

    [PreserveSig]
    int SetString(int tag, string str);

    [PreserveSig]
    int SetExtensionCommand(int tag, string extension);

    [PreserveSig]
    int SetUlong();

    [PreserveSig]
    int SetUlong64();

    [PreserveSig]
    int SetBuffer();

    [PreserveSig]
    int AddString();

    [PreserveSig]
    int AddExtensionCommand();

    [PreserveSig]
    int AddUlong();

    [PreserveSig]
    int AddUlong64();

    [PreserveSig]
    int AddBuffer();

    [PreserveSig]
    int GetDebugFATagControl();

    [PreserveSig]
    int GetAnalysisXml(out IXMLDOMElement ppXMLDOMElement);

    // Adds another analysis object as structured data in a new entry
    int AddStructuredAnalysisData();
}

[ComImport]
[Guid("2933BF86-7B36-11d2-B20E-00C04F983E60")]
public interface IXMLDOMElement : IXMLDOMNode
{
    int TagName();

    int GetAttribute();

    int SetAttribute();

    int RemoveAttribute();

    int GetAttributeNode();

    int SetAttributeNode();

    int RemoveAttributeNode();

    int GetElementsByTagName();

    int Normalize();
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("2933BF80-7B36-11d2-B20E-00C04F983E60")]
public interface IXMLDOMNode
{
    int NodeName();

    int NodeValue(out object o);

    int NodeValue(object o);

    int NodeType();

    int ParentNode();

    int ChildNodes();

    int FirstChild();

    int LastChild();

    int PreviousSibling();

    int NextSibling();

    int Attributes();

    int InsertBefore();

    int ReplaceChild();

    int RemoveChild();

    int AppendChild();

    int HasChildNodes();

    int OwnerDocument();

    int CloneNode();

    int NodeTypeString();

    int Text(out string s);

    int Text(string s);

    int Specified();

    int Definition();

    int Type1();

    int Type2();

    int Type3();

    int Type4();

    int NodeTypedValue(object o);

    int NodeTypedValue(out object o);

    int DataType(out object o);

    int DataType(object o);

    int Get_xml([MarshalAs(UnmanagedType.BStr)] out string xmlString);

    int TransformNode();

    int SelectNodes();

    int SelectSingleNode();

    int Parsed();

    int NamespaceURI();

    int Prefix();

    int BaseName();

    int TransformNodeToObject();
}
