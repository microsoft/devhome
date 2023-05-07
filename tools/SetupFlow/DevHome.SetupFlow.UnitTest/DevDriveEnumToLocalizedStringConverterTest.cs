// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.Utilities;

namespace DevHome.SetupFlow.UnitTest;

/// <summary>
/// Test class for the DevDriveEnumToLocalizedStringConverter which takes a DevDriveValidationResult
/// and converts it to a localized string within the setup flow Resources.resw file.
/// </summary>
[TestClass]
public class DevDriveEnumToLocalizedStringConverterTest : BaseSetupFlowTest
{
    public DevDriveEnumToLocalizedStringConverter Converter => new (StringResource.Object);

    [TestMethod]
    public void InvalidDriveSizeEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.InvalidDriveSize, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveInvalidDriveSize);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }

    [TestMethod]
    public void InvalidDriveLabelEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.InvalidDriveLabel, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveInvalidDriveLabel);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }

    [TestMethod]
    public void InvalidFolderLocationEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.InvalidFolderLocation, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveInvalidFolderLocation);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }

    [TestMethod]
    public void FileNameAlreadyExistsEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.FileNameAlreadyExists, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveFileNameAlreadyExists);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }

    [TestMethod]
    public void DriveLetterNotAvailableEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.DriveLetterNotAvailable, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveDriveLetterNotAvailable);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }

    [TestMethod]
    public void NoDriveLettersAvailableEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.NoDriveLettersAvailable, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveNoDriveLettersAvailable);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }

    [TestMethod]
    public void NotEnoughFreeSpaceEnumToLocalizedString()
    {
        var converterResultStringID = Converter.Convert(DevDriveValidationResult.NotEnoughFreeSpace, null, null, string.Empty);
        var expectedLocalizedStringID = StringResource.Object.GetLocalized(StringResourceKey.DevDriveNotEnoughFreeSpace);
        Assert.AreEqual(expectedLocalizedStringID, converterResultStringID);
    }
}
