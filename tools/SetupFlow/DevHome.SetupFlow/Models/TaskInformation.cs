// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.Models;

public partial class TaskInformation : ObservableObject
{
    public int TaskIndex
    {
        get; set;
    }

    public ISetupTask TaskToExecute
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the message to display in the loading screen.
    /// </summary>
    public string MessageToShow
    {
        get; set;
    }
}
