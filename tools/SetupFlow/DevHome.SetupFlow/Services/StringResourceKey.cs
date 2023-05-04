// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Static class for storing the keys of the string resources that are accessed
/// from C# such as string resources that have placeholders or data that is
/// defined in the code and will surface on the UI.
/// </summary>
public static class StringResourceKey
{
    // Keys in this file should be a subset of the ones found in the .resw file.
    public static readonly string AppInstallerUpdateAvailableCancelButton = nameof(AppInstallerUpdateAvailableCancelButton);
    public static readonly string AppInstallerUpdateAvailableMessage = nameof(AppInstallerUpdateAvailableMessage);
    public static readonly string AppInstallerUpdateAvailableTitle = nameof(AppInstallerUpdateAvailableTitle);
    public static readonly string AppInstallerUpdateAvailableUpdateButton = nameof(AppInstallerUpdateAvailableUpdateButton);
    public static readonly string ApplicationsAddedPlural = nameof(ApplicationsAddedPlural);
    public static readonly string ApplicationsAddedSingular = nameof(ApplicationsAddedSingular);
    public static readonly string Applications = nameof(Applications);
    public static readonly string Basics = nameof(Basics);
    public static readonly string BrowseTextBlock = nameof(BrowseTextBlock);
    public static readonly string Close = nameof(Close);
    public static readonly string ConfigurationFileApplyError = nameof(ConfigurationFileApplyError);
    public static readonly string ConfigurationFileApplySuccess = nameof(ConfigurationFileApplySuccess);
    public static readonly string ConfigurationFileApplySuccessReboot = nameof(ConfigurationFileApplySuccessReboot);
    public static readonly string ConfigurationFileApplying = nameof(ConfigurationFileApplying);
    public static readonly string ConfigurationFieldInvalid = nameof(ConfigurationFieldInvalid);
    public static readonly string ConfigurationFileInvalid = nameof(ConfigurationFileInvalid);
    public static readonly string ConfigurationFileOpenUnknownError = nameof(ConfigurationFileOpenUnknownError);
    public static readonly string ConfigurationFileVersionUnknown = nameof(ConfigurationFileVersionUnknown);
    public static readonly string ConfigurationFileTypeNotSupported = nameof(ConfigurationFileTypeNotSupported);
    public static readonly string ConfigurationUnitFailed = nameof(ConfigurationUnitFailed);
    public static readonly string ConfigurationUnitSkipped = nameof(ConfigurationUnitSkipped);
    public static readonly string ConfigurationUnitSuccess = nameof(ConfigurationUnitSuccess);
    public static readonly string ConfigurationUnitSummary = nameof(ConfigurationUnitSummary);
    public static readonly string ConfigurationUnitStats = nameof(ConfigurationUnitStats);
    public static readonly string ConfigurationViewTitle = nameof(ConfigurationViewTitle);
    public static readonly string DevDriveReviewTitle = nameof(DevDriveReviewTitle);
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
    public static readonly string DevDriveUnableToCreateError = nameof(DevDriveUnableToCreateError);
    public static readonly string DevDriveWindowByteUnitComboBoxGB = nameof(DevDriveWindowByteUnitComboBoxGB);
    public static readonly string DevDriveWindowByteUnitComboBoxTB = nameof(DevDriveWindowByteUnitComboBoxTB);
    public static readonly string EditClonePathDialog = nameof(EditClonePathDialog);
    public static readonly string EditClonePathDialogUncheckCheckMark = nameof(EditClonePathDialogUncheckCheckMark);
    public static readonly string FileTypeNotSupported = nameof(FileTypeNotSupported);
    public static readonly string InstalledPackage = nameof(InstalledPackage);
    public static readonly string InstalledPackageReboot = nameof(InstalledPackageReboot);
    public static readonly string InstallingPackage = nameof(InstallingPackage);
    public static readonly string InstallPackageErrorBlockedByPolicy = nameof(InstallPackageErrorBlockedByPolicy);
    public static readonly string InstallPackageErrorDownloadError = nameof(InstallPackageErrorDownloadError);
    public static readonly string InstallPackageErrorInternalError = nameof(InstallPackageErrorInternalError);
    public static readonly string InstallPackageErrorInstallError = nameof(InstallPackageErrorInstallError);
    public static readonly string InstallPackageErrorNoApplicableInstallers = nameof(InstallPackageErrorNoApplicableInstallers);
    public static readonly string InstallPackageError = nameof(InstallPackageError);
    public static readonly string InstallPackageErrorUnknownError = nameof(InstallPackageErrorUnknownError);
    public static readonly string Next = nameof(Next);
    public static readonly string NoSearchResultsFoundTitle = nameof(NoSearchResultsFoundTitle);
    public static readonly string PackagesCount = nameof(PackagesCount);
    public static readonly string PackageDescriptionThreeParts = nameof(PackageDescriptionThreeParts);
    public static readonly string PackageDescriptionTwoParts = nameof(PackageDescriptionTwoParts);
    public static readonly string PackageInstalledTooltip = nameof(PackageInstalledTooltip);
    public static readonly string PackageNameTooltip = nameof(PackageNameTooltip);
    public static readonly string PackagePublisherNameTooltip = nameof(PackagePublisherNameTooltip);
    public static readonly string PackageSourceTooltip = nameof(PackageSourceTooltip);
    public static readonly string PackageVersionTooltip = nameof(PackageVersionTooltip);
    public static readonly string PathWithColon = nameof(PathWithColon);
    public static readonly string ResultCount = nameof(ResultCount);
    public static readonly string RestorePackagesTitle = nameof(RestorePackagesTitle);
    public static readonly string RestorePackagesDescription = nameof(RestorePackagesDescription);
    public static readonly string Repository = nameof(Repository);
    public static readonly string ReviewNothingToSetUpToolTip = nameof(ReviewNothingToSetUpToolTip);
    public static readonly string SelectedPackagesCount = nameof(SelectedPackagesCount);
    public static readonly string SetUpButton = nameof(SetUpButton);
    public static readonly string SizeWithColon = nameof(SizeWithColon);
    public static readonly string LoadingExecutingProgress = nameof(LoadingExecutingProgress);
    public static readonly string ActionCenterDisplay = nameof(ActionCenterDisplay);
    public static readonly string NeedsRebootMessage = nameof(NeedsRebootMessage);
    public static readonly string ApplicationsPageTitle = nameof(ApplicationsPageTitle);
    public static readonly string ReposConfigPageTitle = nameof(ReposConfigPageTitle);
    public static readonly string ReviewPageTitle = nameof(ReviewPageTitle);

    // Repository loading screen messages
    public static readonly string CloneRepoCreating = nameof(CloneRepoCreating);
    public static readonly string CloneRepoCreated = nameof(CloneRepoCreated);
    public static readonly string CloneRepoError = nameof(CloneRepoError);
    public static readonly string CloneRepoErrorForActionCenter = nameof(CloneRepoErrorForActionCenter);
    public static readonly string CloneRepoRestart = nameof(CloneRepoRestart);

    // Configure task loading screen messages
    public static readonly string ConfigureTaskCreating = nameof(ConfigureTaskCreating);
    public static readonly string ConfigureTaskCreated = nameof(ConfigureTaskCreated);
    public static readonly string ConfigureTaskError = nameof(ConfigureTaskError);
    public static readonly string ConfigureTaskRestart = nameof(ConfigureTaskRestart);

    // App download loading screen messages
    public static readonly string DownloadAppCreating = nameof(DownloadAppCreating);
    public static readonly string DownloadAppCreated = nameof(DownloadAppCreated);
    public static readonly string DownloadAppError = nameof(DownloadAppError);
    public static readonly string DownloadAppRestart = nameof(DownloadAppRestart);

    // Dev drive loading screen messages
    public static readonly string DevDriveCreating = nameof(DevDriveCreating);
    public static readonly string DevDriveCreated = nameof(DevDriveCreated);
    public static readonly string DevDriveErrorWithReason = nameof(DevDriveErrorWithReason);
    public static readonly string DevDriveRestart = nameof(DevDriveRestart);

    // Loading screen
    public static readonly string LoadingScreenActionCenterErrors = nameof(LoadingScreenActionCenterErrors);
    public static readonly string LoadingPageSteps = nameof(LoadingPageSteps);
    public static readonly string LoadingScreenGoToSummaryButtonContent = nameof(LoadingScreenGoToSummaryButtonContent);

    // Repo tool
    public static readonly string RepoDialogName = nameof(RepoDialogName);
    public static readonly string RepoToolNextButtonTooltip = nameof(RepoToolNextButtonTooltip);
    public static readonly string RepoAccountPagePrimaryButtonText = nameof(RepoAccountPagePrimaryButtonText);
    public static readonly string RepoEverythingElsePrimaryButtonText = nameof(RepoEverythingElsePrimaryButtonText);
    public static readonly string RepoPageEditClonePathAutomationProperties = nameof(RepoPageEditClonePathAutomationProperties);
    public static readonly string RepoPageRemoveRepoAutomationProperties = nameof(RepoPageRemoveRepoAutomationProperties);
    public static readonly string ClonePathNotFullyQualifiedMessage = nameof(ClonePathNotFullyQualifiedMessage);
    public static readonly string ClonePathNotFolder = nameof(ClonePathNotFolder);
    public static readonly string ClonePathDriveDoesNotExist = nameof(ClonePathDriveDoesNotExist);
    public static readonly string EditClonePathDialogName = nameof(EditClonePathDialogName);

    // Url Validation
    public static readonly string UrlValidationEmpty = nameof(UrlValidationEmpty);
    public static readonly string UrlValidationBadUrl = nameof(UrlValidationBadUrl);
    public static readonly string UrlValidationNotFound = nameof(UrlValidationNotFound);
    public static readonly string UrlValidationRepoAlreadyAdded = nameof(UrlValidationRepoAlreadyAdded);
}
