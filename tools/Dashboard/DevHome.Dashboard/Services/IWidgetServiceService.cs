// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using static DevHome.Dashboard.Services.WidgetServiceService;

namespace DevHome.Dashboard.Services;

public interface IWidgetServiceService
{
    /// <summary>
    /// Checks whether a WidgetService is installed on the machine.
    /// </summary>
    /// <returns>Returns true if there is a valid WidgetService that meets the minimum required version, otherwise false.</returns>
    public bool CheckForWidgetService();

    public Task<bool> TryInstallingWidgetService();

    public WidgetServiceStates GetWidgetServiceState();
}
