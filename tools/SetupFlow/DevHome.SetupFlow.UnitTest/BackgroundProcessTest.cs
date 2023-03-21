// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ElevatedComponent;

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
    [TestMethod]
    public void BackgroundProcessIPCSetup()
    {
        (var remoteElevatedFactory, var backgroundProcess) =
            IPCSetup.CreateOutOfProcessObjectAndGetProcess<IElevatedComponentFactory>(isForTesting: true);
        Assert.IsFalse(backgroundProcess.HasExited, "Process should still be running right after creation");

        // Write a random string on the background process
        var randomString = Guid.NewGuid().ToString();
        remoteElevatedFactory.Value.WriteToStdOut(randomString);
        Assert.IsFalse(backgroundProcess.HasExited, "Process should still be running after calling a method");

        // Release the completion mutex to signal the process to exit
        var waitForExit = new EventWaitHandle(initialState: false, EventResetMode.AutoReset);
        backgroundProcess.Exited += (_, _) =>
        {
            waitForExit.Set();
        };

        remoteElevatedFactory.Dispose();

        // Confirm the process exits and it had the expected output
        Assert.IsTrue(waitForExit.WaitOne(1000), "Process should exit after disposing the completion mutex");
        Assert.IsTrue(backgroundProcess.HasExited, "Process should exit after releasing the completion mutex");
        Assert.AreEqual(randomString, backgroundProcess.StandardOutput.ReadLine(), "Process should have written to its stdout");
    }
}
