// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ServiceProcess;
using HyperVExtension.Models;

namespace HyperVExtension.UnitTest.Mocks;

public class WindowsServiceControllerMock : IWindowsServiceController
{
    public ServiceControllerStatus Status => MockStatus;

    public ServiceControllerStatus MockStatus { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public WindowsServiceControllerMock(ServiceControllerStatus status)
    {
        MockStatus = status;
    }

    public void ContinueService()
    {
    }

    public void StartService()
    {
    }

    public void WaitForStatusChange(ServiceControllerStatus desiredStatus, TimeSpan timeout)
    {
        // simulate us attempting to change the status of a service and timing out.
        if (Status != ServiceControllerStatus.Running)
        {
            throw new System.ServiceProcess.TimeoutException();
        }
    }
}
