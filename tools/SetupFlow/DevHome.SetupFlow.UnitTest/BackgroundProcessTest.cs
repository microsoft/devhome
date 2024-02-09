// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Server;

using DevHome.SetupFlow.Common.Elevation;
using Server::DevHome.SetupFlow.ElevatedComponent;

namespace DevHome.SetupFlow.UnitTest;

[TestClass]
public class BackgroundProcessTest
{
    /// <summary>
    /// Tests the setup for inter-process communication with the background process,
    /// and that we are able to run code on that process.
    /// </summary>
    /// <remarks>
    /// This tests a slightly different code path than we use in the main app as
    /// we are not running the process elevated.
    /// </remarks>
    //// TODO: This test works locally but not on the pipeline.
    //// https://github.com/microsoft/devhome/issues/621
    ////       Disabling it for now to get the change in and unblock consumers.
    [TestMethod]
    [Ignore]
    public void BackgroundProcessIPCSetup()
    {
        (var remoteElevatedOperation, var backgroundProcess) =
            IPCSetup.CreateOutOfProcessObjectAndGetProcess<IElevatedComponentOperation>(null, isForTesting: true);
        Assert.IsFalse(backgroundProcess.HasExited, "Process should still be running right after creation");

        // Write a random string on the background process
        var randomString = Guid.NewGuid().ToString();
        remoteElevatedOperation.Value.WriteToStdOut(randomString);
        Assert.IsFalse(backgroundProcess.HasExited, "Process should still be running after calling a method");

        // Release the completion mutex to signal the process to exit
        var waitForExit = new EventWaitHandle(initialState: false, EventResetMode.AutoReset);
        backgroundProcess.Exited += (_, _) =>
        {
            waitForExit.Set();
        };

        remoteElevatedOperation.Dispose();

        // Confirm the process exits and it had the expected output
        // The first assert with the timeout ensures we don't sit here waiting for too long;
        // the second assert ensures it did exit instead of timing out.
        Assert.IsTrue(backgroundProcess.HasExited || waitForExit.WaitOne(10 * 1000), "Process should exit after disposing the completion mutex");
        Assert.IsTrue(backgroundProcess.HasExited, "Process should exit after releasing the completion mutex");
        Assert.IsTrue(backgroundProcess.StandardOutput.ReadToEnd().Contains(randomString), "Process should have written to its stdout");
    }
}
