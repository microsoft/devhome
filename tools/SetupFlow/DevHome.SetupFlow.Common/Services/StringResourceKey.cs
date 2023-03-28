// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.Common.Services;

/// <summary>
/// Static class for storing the keys of the string resources that are accessed
/// from C# such as string resources that have placeholders or data that is
/// defined in the code and will surface on the UI.
/// </summary>
public static class StringResourceKey
{
    // Keys in this file should be a subset of the ones found in the .resw file.
    public static readonly string ApplicationsSelectedCount = nameof(ApplicationsSelectedCount);
    public static readonly string Applications = nameof(Applications);
    public static readonly string Basics = nameof(Basics);
    public static readonly string BrowseTextBlock = nameof(BrowseTextBlock);
    public static readonly string Close = nameof(Close);
    public static readonly string ConfigurationFileTypeNotSupported = nameof(ConfigurationFileTypeNotSupported);
    public static readonly string DevDriveDefaultFileName = nameof(DevDriveDefaultFileName);
    public static readonly string DevDriveDefaultFolderName = nameof(DevDriveDefaultFolderName);
    public static readonly string DevDriveFilenameAlreadyExists = nameof(DevDriveFilenameAlreadyExists);
    public static readonly string DevDriveInvalidDriveLabel = nameof(DevDriveInvalidDriveLabel);
    public static readonly string DevDriveInvalidDriveSize = nameof(DevDriveInvalidDriveSize);
    public static readonly string DevDriveInvalidFolderLocation = nameof(DevDriveInvalidFolderLocation);
    public static readonly string DevDriveNoDriveLettersAvailable = nameof(DevDriveNoDriveLettersAvailable);
    public static readonly string DevDriveNotEnoughFreeSpace = nameof(DevDriveNotEnoughFreeSpace);
    public static readonly string DevDriveReviewPageNumberOfDevDrives = nameof(DevDriveReviewPageNumberOfDevDrives);
    public static readonly string DevDriveReviewPageNumberOfDevDrivesTitle = nameof(DevDriveReviewPageNumberOfDevDrivesTitle);
    public static readonly string DevDriveWindowByteUnitComboBoxGB = nameof(DevDriveWindowByteUnitComboBoxGB);
    public static readonly string DevDriveWindowByteUnitComboBoxTB = nameof(DevDriveWindowByteUnitComboBoxTB);
    public static readonly string FileTypeNotSupported = nameof(FileTypeNotSupported);
    public static readonly string InstalledPackage = nameof(InstalledPackage);
    public static readonly string InstallingPackage = nameof(InstallingPackage);
    public static readonly string InstallPackageErrorBlockedByPolicy = nameof(InstallPackageErrorBlockedByPolicy);
    public static readonly string InstallPackageErrorDownloadError = nameof(InstallPackageErrorDownloadError);
    public static readonly string InstallPackageErrorInternalError = nameof(InstallPackageErrorInternalError);
    public static readonly string InstallPackageErrorInstallError = nameof(InstallPackageErrorInstallError);
    public static readonly string InstallPackageErrorNoApplicableInstallers = nameof(InstallPackageErrorNoApplicableInstallers);
    public static readonly string InstallPackageErrorWithReason = nameof(InstallPackageErrorWithReason);
    public static readonly string InstallPackageErrorUnknownError = nameof(InstallPackageErrorUnknownError);
    public static readonly string Next = nameof(Next);
    public static readonly string NoSearchResultsFoundTitle = nameof(NoSearchResultsFoundTitle);
    public static readonly string PackagesCount = nameof(PackagesCount);
    public static readonly string PathWithColon = nameof(PathWithColon);
    public static readonly string ResultCount = nameof(ResultCount);
    public static readonly string RestorePackagesTitle = nameof(RestorePackagesTitle);
    public static readonly string RestorePackagesDescription = nameof(RestorePackagesDescription);
    public static readonly string Repository = nameof(Repository);
    public static readonly string ReviewNothingToSetUpToolTip = nameof(ReviewNothingToSetUpToolTip);
    public static readonly string SelectedPackagesCount = nameof(SelectedPackagesCount);
    public static readonly string SetUpButton = nameof(SetUpButton);
    public static readonly string SizeWithColon = nameof(SizeWithColon);
    public static readonly string ViewConfiguration = nameof(ViewConfiguration);

    public static readonly string ApplicationsPageTitle = nameof(ApplicationsPageTitle);
    public static readonly string ReposConfigPageTitle = nameof(ReposConfigPageTitle);
    public static readonly string ReviewPageTitle = nameof(ReviewPageTitle);
}
