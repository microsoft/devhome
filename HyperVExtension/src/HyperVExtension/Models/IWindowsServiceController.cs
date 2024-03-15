// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ServiceProcess;

namespace HyperVExtension.Models;

/// <summary>
/// Wrapper interface for the Service Controller class.
/// </summary>
public interface IWindowsServiceController
{
    public ServiceControllerStatus Status { get; }

    public string ServiceName { get; set; }

    public void ContinueService();

    public void StartService();

    public void WaitForStatusChange(ServiceControllerStatus desiredStatus, TimeSpan timeout);
}
