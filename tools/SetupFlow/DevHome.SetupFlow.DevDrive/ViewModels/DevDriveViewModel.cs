// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.DevDrive.Models;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.DevDrive.Utilities;
using DevHome.SetupFlow.DevDrive.Windows;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.DevDrive.ViewModels;

public partial class DevDriveViewModel : ObservableObject, IDevDriveWindowViewModel
{
    private readonly ILogger _logger;
    private readonly DevDriveTaskGroup _taskGroup;
    private readonly IDevDriveManager _devDriveManager;
    private readonly Models.DevDrive _concreteDevDrive = new ();
    private readonly IReadOnlyList<ByteUnit> _byteUnitList = new List<ByteUnit>
    {
        ByteUnit.GB,
        ByteUnit.TB,
    };

    /// <summary>
    /// Gets the window that will contain the view.
    /// </summary>
    public DevDriveWindow DevDriveWindowContainer
    {
        get; private set;
    }

    public IDevDrive AssociatedDrive => _concreteDevDrive;

    /// <summary>
    /// Gets the drive letters available on the system.
    /// </summary>
    public IList<char> DriveLetters => DevDriveUtil.GetAvailableDriveLetters();

    public DevDriveViewModel(
        IHost host,
        ILogger logger,
        DevDriveTaskGroup taskGroup)
    {
        _logger = logger;
        _taskGroup = taskGroup;
        _devDriveManager = host.GetService<IDevDriveManager>();
    }

    /// <summary>
    /// Dev Drive name textbox. This name will be used as the label for the eventual Dev Drive.
    /// This is limited to 32 characters (a Windows limitation).
    /// </summary>
    [ObservableProperty]
    private string _name;

    /// <summary>
    /// Dev Drive location textbox. This is the location that we will save the virtual disk file to.
    /// </summary>
    [ObservableProperty]
    private string _location;

    /// <summary>
    /// Dev Drive size. This is the size the Dev Drive will be created with. This along with
    /// the value selected in the byteUnitList will tell us the exact size the user wants to create
    /// their Dev Drive to have e.g if this value is 50 and the byteUnitList is GB, the user wants the drive to be 50 GB in size.
    /// </summary>
    [ObservableProperty]
    private ulong _size;

    /// <summary>
    /// Byte unit of mearsure combo box index.
    /// </summary>
    [ObservableProperty]
    private int _comboBoxByteUnitIndex;

    /// <summary>
    /// Drive letter combo box.
    /// </summary>
    [ObservableProperty]
    private int _comboBoxDriveLetterIndex;

    /// <summary>
    /// Cancel button click command.
    /// </summary>
    [RelayCommand]
    private void CancelButton()
    {
        DevDriveWindowContainer.Close();
    }

    /// <summary>
    /// Save button click command.
    /// </summary>
    [RelayCommand]
    private void SaveButton()
    {
        ByteUnit driveUnitOfMeasure = _byteUnitList[_comboBoxByteUnitIndex];
        _concreteDevDrive.DriveLetter = DriveLetters[_comboBoxDriveLetterIndex];
        _concreteDevDrive.DriveSizeInBytes = DevDriveUtil.ConvertToBytes(Size, driveUnitOfMeasure);
        _concreteDevDrive.DriveLocation = _location;
        _concreteDevDrive.DriveLabel = _name;

        if (DevDriveUtil.ValidateDevDrive(_concreteDevDrive))
        {
            _concreteDevDrive.State = DevDriveState.New;
            DevDriveWindowContainer.Close();
        }
        else
        {
             // ShowErrorInUI();
        }
    }

    public Task<bool> LaunchDevDriveWindow()
    {
        DevDriveWindowContainer = new (this);
        DevDriveWindowContainer.Closed += ViewContainerClosed;
        return Task.FromResult(DevDriveWindowContainer.Show());
    }

    private void ViewContainerClosed(object sender, WindowEventArgs args)
    {
        _devDriveManager.NotifyDevDriveWindowClosed(_concreteDevDrive);
    }

    public void ShowErrorInUI() => throw new NotImplementedException();
}
