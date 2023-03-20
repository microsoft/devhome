// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace DevHome.SetupFlow.ElevatedComponent;

/// <summary>
/// Helper class for establishing a background process to offload work to,
/// and communication with it. This is currently used only when we need
/// some tasks to be executed with admin permissions; in that case we
/// create a background process with the required permissions and then
/// hand off all the work to it.
/// </summary>
/// <remarks>
/// For this setup we need two things, first is having inter-process
/// communication between the processes and the second is having the
/// background process run elevated. There are multiple ways to get
/// this to work (e.g. using COM and creating objects with the COM
/// Elevation Moniker).
///
/// The solution we use here is to start the elevated process, and then
/// use a shared block of memory to pass the marshalling info for a
/// WinRT object that will then be used to create all the required objects
/// for communication.
/// </remarks>
//// Implementation details:
////
//// * The background process executable has an app manifest that requires
////   it to always run elevated.
////
//// * We use a MemoryMappedFile to share a block of memory between the
////   app process and the background process we start. On this block we
////   write, in order: an HResult to report failures, the size of the
////   marshall information for the factory object, and finally the
////   marshaler object itself.
////
//// * To have the main app process wait for the initialization done in the
////   background process to finish, it creates a global EventWaitHandle
////   that is signaled by the background process.
////
//// * To prevent the background process from terminating right after the
////   setup while we still have objects hosted on it, the main app
////   process creates and acquires a global mutex and only releases when
////   the factory object is not needed anymore. The background process
////   waits to acquire the mutex before exiting, ensuring that it only
////   terminates when it is no longer needed.
////
//// * The methods that set up the remote object are generic due to some
////   behaviors of CsWinRT we need to work around. The ElevatedServer
////   process needs the actual types from the ElevatedComponent to
////   create the new object, so it has a project reference to it.
////   Everywhere else, what we need is the projection types to be able
////   to create the proxy objects; so we use a reference to the
////   ElevatedComponent.Projection that uses the WinMD to generate the
////   projections. The code here is called from both sides, so it needs
////   to work for different versions of the same type
////
//// TODO: Copy winmd
public static class IPCSetup
{
    /// <summary>
    /// Object that is written at the beginning of the shared memory block.
    /// The marshalled factory object is written immediately after this.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MappedMemoryValue
    {
        /// <summary>
        /// Result of the setup operation.
        /// </summary>
        public int HResult;

        /// <summary>
        /// Size of the marshaled factory object.
        /// </summary>
        public long MarshaledObjectSize;
    }

    /// <summary>
    /// Maximum capacity of the shared memory block. Must be at least big
    /// enough to hold a <see cref="MappedMemoryValue"/> and the marshalled
    /// factory object. Default to 4kb.
    /// </summary>
    private const long MappedMemoryCapacityInBytes = 4 << 10;

    /// <summary>
    /// The size of the <see cref="MappedMemoryValue"/> that is written
    /// at the beginning of the shared memory.
    /// </summary>
    private static readonly long MappedMemoryValueSizeInBytes = Marshal.SizeOf<MappedMemoryValue>();

    /// <summary>
    /// The maximum size that the marshalled factory object can have.
    /// If this is not big enough, we should increase the maximum capacity.
    /// </summary>
    private static readonly long MaxFactorySizeInBytes = MappedMemoryCapacityInBytes - MappedMemoryValueSizeInBytes;

    /// <summary>
    /// Gets the Interface ID for a type; used for the initial interface being marshalled between the processes.
    /// </summary>
    private static Guid GetMarshalInterfaceGUID<T>()
    {
        return GuidGenerator.CreateIID(typeof(T));
    }

    /// <summary>
    /// Creates a factory for the objects that need to run in the elevated
    /// background process. This is to be called from the (unelevated) main
    /// app process.
    /// </summary>
    /// <returns>A factory that creates WinRT objects in the background process.</returns>
    public static RemoteObject<T> CreateOutOfProcessObject<T>()
    {
        (var remoteObject, _) = CreateOutOfProcessObjectAndGetProcess<T>(runBackgroundProcessElevated: true);
        return remoteObject;
    }

    /// <summary>
    /// Creates a factory for the objects that need to run in a
    /// background process. This is to be called from the main
    /// app process.
    /// </summary>
    /// <remarks>
    /// This is intended to be used for tests. For anything else we
    /// should use <see cref="IPCSetup.CreateOutOfProcessObject{T}"/>
    /// </remarks>
    /// <returns>
    /// A factory that creates WinRT objects in the background process,
    /// and the process object for the background process.
    /// </returns>
    public static (RemoteObject<T>, Process) CreateOutOfProcessObjectAndGetProcess<T>(bool runBackgroundProcessElevated)
    {
        // The shared memory block, initialization event and completion mutex all need a name
        // that will be used by the child process to find them. We use new random GUIDs for them.
        // For the memory block, we also set the handle inheritability so that only descendant
        // process can access it.
        var mappedFileName = Guid.NewGuid().ToString();
        var initEventName = Guid.NewGuid().ToString();
        var completionMutexName = Guid.NewGuid().ToString();

        // Create shared memory block.
        var mappedFile = MemoryMappedFile.CreateNew(
            mappedFileName,
            MappedMemoryCapacityInBytes,
            MemoryMappedFileAccess.ReadWrite,
            MemoryMappedFileOptions.None,
            HandleInheritability.Inheritable);

        // Write a failure result to the shared memory in case the background process
        // fails without writting anything.
        MappedMemoryValue mappedMemoryValue = default;
        using (var mappedFileAccessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write))
        {
            mappedMemoryValue.HResult = unchecked((int)0x80000008); // E_FAIL
            mappedFileAccessor.Write(0, ref mappedMemoryValue);
        }

        // Create an event that the background process will signal to indicate it has completed
        // creating the object and writing it to the shared block.
        var initEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, initEventName);

        // Create a mutex to hold on to to ensure keep the background process alive.
        var completionMutex = new Mutex(initiallyOwned: true, completionMutexName);

        // Start the elevated process.
        // Command is: <server>.exe <mapped memory name> <event name> <mutex name>
        var serverArgs = $"{mappedFileName} {initEventName} {completionMutexName}";
        var serverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevHome.SetupFlow.ElevatedServer.exe");

        var processStartupInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = serverArgs,
            CreateNoWindow = true,
        };

        if (runBackgroundProcessElevated)
        {
            // We need to start the process with ShellExecute to run elevated
            processStartupInfo.UseShellExecute = true;
            processStartupInfo.Verb = "runas";
        }
        else
        {
            // If we are not running elevated, we can run without ShellExecute
            // and get the process output. This is useful for testing.
            processStartupInfo.UseShellExecute = false;
            processStartupInfo.RedirectStandardOutput = true;
        }

        var process = Process.Start(processStartupInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start background process");
        }

        // Wait for the background process to finish initializing the object and writing
        // it to the shared memory. The timeout is arbitrary and can be changed.
        // We also stop waiting if the process exits.
        process.Exited += (_, _) => { initEvent.Set(); };
        if (!initEvent.WaitOne(60 * 1000))
        {
            throw new TimeoutException("Background process failed to initialized in the allowed time");
        }

        if (process.HasExited)
        {
            throw new InvalidOperationException("Background process terminated");
        }

        // Read the initialization result and the factory size
        using (var mappedFileAccessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
        {
            mappedFileAccessor.Read(0, out mappedMemoryValue);
            Marshal.ThrowExceptionForHR(mappedMemoryValue.HResult);
        }

        // Read the marshalling object
        Marshal.ThrowExceptionForHR(PInvoke.CreateStreamOnHGlobal(0, fDeleteOnRelease: true, out var stream));

        using (var mappedFileAccessor = mappedFile.CreateViewAccessor())
        {
            unsafe
            {
                // Copy the object into an IStream that we can use with CoUnmarshalInterface
                byte* rawPointer = null;
                uint bytesWritten;
                try
                {
                    mappedFileAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref rawPointer);
                    Marshal.ThrowExceptionForHR(stream.Write(rawPointer + MappedMemoryValueSizeInBytes, (uint)mappedMemoryValue.MarshaledObjectSize, &bytesWritten));
                }
                finally
                {
                    if (rawPointer != null)
                    {
                        mappedFileAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    }
                }

                if (bytesWritten != mappedMemoryValue.MarshaledObjectSize)
                {
                    throw new InvalidDataException("Shared memory stream has unexpected data");
                }

                // Reset the stream to the beginning before reading the object
                stream.Seek(0, SeekOrigin.Begin);
            }
        }

        Marshal.ThrowExceptionForHR(PInvoke.CoUnmarshalInterface(stream, GetMarshalInterfaceGUID<T>(), out var obj));
        var value = MarshalInterface<T>.FromAbi(Marshal.GetIUnknownForObject(obj));

        return (new RemoteObject<T>(value, completionMutex), process);
    }

    /// <summary>
    /// Completes the remote object initialization from the background process.
    /// This means writing the result to the shared memory block, signaling
    /// to the caller that the initialization is complete, and waiting for
    /// the completion mutex to be released to signal that we can return.
    /// This is to be called from the elevated background process.
    /// </summary>
    /// <param name="defaultHResult">HResult to write to the shared memory.</param>
    /// <param name="value">
    /// The object to write on the shared memory, can be null when writing a failure.
    /// </param>
    public static void CompleteRemoteObjectInitialization<T>(
        int defaultHResult,
        T? value,
        string mappedFileName,
        string initEventName,
        string completionMutexName)
    {
        // Open the shared resources
        var mappedFile = MemoryMappedFile.OpenExisting(mappedFileName, MemoryMappedFileRights.Write);
        var initEvent = EventWaitHandle.OpenExisting(initEventName);
        var completionMutex = Mutex.OpenExisting(completionMutexName);

        MappedMemoryValue mappedMemory = default;

        try
        {
            // Only read the object for non-error cases
            Marshal.ThrowExceptionForHR(defaultHResult);
            if (value is not null)
            {
                unsafe
                {
                    // Write the object into a stream from which we will copy to the shared memory
                    Marshal.ThrowExceptionForHR(PInvoke.CreateStreamOnHGlobal(0, fDeleteOnRelease: true, out var stream));

                    var marshaler = MarshalInterface<T>.CreateMarshaler(value);
                    var marshalerAbi = MarshalInterface<T>.GetAbi(marshaler);
                    Marshal.ThrowExceptionForHR(PInvoke.CoMarshalInterface(stream, GetMarshalInterfaceGUID<T>(), Marshal.GetObjectForIUnknown(marshalerAbi), (uint)MSHCTX.MSHCTX_LOCAL, null, (uint)MSHLFLAGS.MSHLFLAGS_NORMAL));

                    // Store the object size
                    ulong streamSize;
                    stream.Seek(0, SeekOrigin.Current, &streamSize);
                    mappedMemory.MarshaledObjectSize = (long)streamSize;

                    if (mappedMemory.MarshaledObjectSize > MaxFactorySizeInBytes)
                    {
                        throw new InvalidDataException("Marshaled object is too large for shared memory block");
                    }

                    // Reset the stream to the beginning before reading it to the shared memory.
                    stream.Seek(0, SeekOrigin.Begin);

                    using var mappedFileAccessor = mappedFile.CreateViewAccessor();
                    byte* rawPointer = null;
                    uint bytesRead;
                    try
                    {
                        mappedFileAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref rawPointer);
                        Marshal.ThrowExceptionForHR(stream.Read(rawPointer + MappedMemoryValueSizeInBytes, (uint)streamSize, &bytesRead));
                    }
                    finally
                    {
                        if (rawPointer != null)
                        {
                            mappedFileAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }

                    if (bytesRead != streamSize)
                    {
                        throw new InvalidDataException("Failed to write marshal object to shared memory");
                    }
                }
            }
        }
        catch (Exception e)
        {
            mappedMemory.HResult = e.HResult;
        }

        // Write the init result and if needed the factory object size.
        using (var accessor = mappedFile.CreateViewAccessor())
        {
            accessor.Write(0, ref mappedMemory);
        }

        // Signal to the caller that we finished initialization.
        initEvent.Set();

        // Wait until the caller releases the object
        completionMutex.WaitOne();
    }
}
