// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Static class for storing the keys of the string resources that are accessed
/// from C# such as string resources that have placeholders or data that is
/// defined in the code and will surface on the UI.
/// </summary>
public static class StringResourceKey
{
    // Keys in this file should be a subset of the ones found in the .resw file.
    public static readonly string AddAllApplications = nameof(AddAllApplications);
    public static readonly string AddApplication = nameof(AddApplication);
    public static readonly string AddedApplication = nameof(AddedApplication);
    public static readonly string AppInstallActivationTitle = nameof(AppInstallActivationTitle);
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
    public static readonly string ConfigurationUnitFailed = nameof(ConfigurationUnitFailed);
    public static readonly string ConfigurationUnitSkipped = nameof(ConfigurationUnitSkipped);
    public static readonly string ConfigurationUnitSuccess = nameof(ConfigurationUnitSuccess);
    public static readonly string ConfigurationUnitSummaryFull = nameof(ConfigurationUnitSummaryFull);
    public static readonly string ConfigurationUnitSummaryMinimal = nameof(ConfigurationUnitSummaryMinimal);
    public static readonly string ConfigurationUnitSummaryNoDescription = nameof(ConfigurationUnitSummaryNoDescription);
    public static readonly string ConfigurationUnitSummaryNoId = nameof(ConfigurationUnitSummaryNoId);
    public static readonly string ConfigurationUnitStats = nameof(ConfigurationUnitStats);
    public static readonly string ConfigurationViewTitle = nameof(ConfigurationViewTitle);
    public static readonly string ConfigurationActivationFailedDisabled = nameof(ConfigurationActivationFailedDisabled);
    public static readonly string ConfigurationActivationFailedBusy = nameof(ConfigurationActivationFailedBusy);
    public static readonly string DevDriveReviewTitle = nameof(DevDriveReviewTitle);
    public static readonly string DevDriveDefaultFileName = nameof(DevDriveDefaultFileName);
    public static readonly string DevDriveDefaultFolderName = nameof(DevDriveDefaultFolderName);
    public static readonly string DevDriveDriveLetterNotAvailable = nameof(DevDriveDriveLetterNotAvailable);
    public static readonly string DevDriveFileNameAlreadyExists = nameof(DevDriveFileNameAlreadyExists);
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
    public static readonly string DoneButton = nameof(DoneButton);
    public static readonly string EditClonePathDialog = nameof(EditClonePathDialog);
    public static readonly string EditClonePathDialogUncheckCheckMark = nameof(EditClonePathDialogUncheckCheckMark);
    public static readonly string FilePickerFileTypeOption = nameof(FilePickerFileTypeOption);
    public static readonly string FilePickerSingleFileTypeOption = nameof(FilePickerSingleFileTypeOption);
    public static readonly string FileTypeNotSupported = nameof(FileTypeNotSupported);
    public static readonly string InstalledPackage = nameof(InstalledPackage);
    public static readonly string InstalledPackageReboot = nameof(InstalledPackageReboot);
    public static readonly string PrepareInstallPackage = nameof(PrepareInstallPackage);
    public static readonly string InstallingPackage = nameof(InstallingPackage);
    public static readonly string DownloadingPackage = nameof(DownloadingPackage);
    public static readonly string InstallationNotesTitle = nameof(InstallationNotesTitle);
    public static readonly string MainPageEnvironmentSetupGroup = nameof(MainPageEnvironmentSetupGroup);
    public static readonly string MainPageQuickConfigurationGroup = nameof(MainPageQuickConfigurationGroup);
    public static readonly string Next = nameof(Next);
    public static readonly string NoSearchResultsFoundTitle = nameof(NoSearchResultsFoundTitle);
    public static readonly string PackagesCount = nameof(PackagesCount);
    public static readonly string PackageDescriptionThreeParts = nameof(PackageDescriptionThreeParts);
    public static readonly string PackageDescriptionTwoParts = nameof(PackageDescriptionTwoParts);
    public static readonly string PackageInstalledTooltip = nameof(PackageInstalledTooltip);
    public static readonly string PackageNameTooltip = nameof(PackageNameTooltip);
    public static readonly string PackageInstalledAnnouncement = nameof(PackageInstalledAnnouncement);
    public static readonly string PackagePublisherNameTooltip = nameof(PackagePublisherNameTooltip);
    public static readonly string PackageSourceTooltip = nameof(PackageSourceTooltip);
    public static readonly string PackageVersionTooltip = nameof(PackageVersionTooltip);
    public static readonly string PathWithColon = nameof(PathWithColon);
    public static readonly string RemoveApplication = nameof(RemoveApplication);
    public static readonly string RemovedApplication = nameof(RemovedApplication);
    public static readonly string RemovedAllApplications = nameof(RemovedAllApplications);
    public static readonly string ResultCountPlural = nameof(ResultCountPlural);
    public static readonly string ResultCountSingular = nameof(ResultCountSingular);
    public static readonly string RestorePackagesTitle = nameof(RestorePackagesTitle);
    public static readonly string RestorePackagesDescription = nameof(RestorePackagesDescription);
    public static readonly string RestorePackagesDescriptionWithDate = nameof(RestorePackagesDescriptionWithDate);
    public static readonly string Repository = nameof(Repository);
    public static readonly string ReviewNothingToSetUpToolTip = nameof(ReviewNothingToSetUpToolTip);
    public static readonly string SelectedPackagesCount = nameof(SelectedPackagesCount);
    public static readonly string SetUpButton = nameof(SetUpButton);
    public static readonly string SizeWithColon = nameof(SizeWithColon);
    public static readonly string URIActivationFailedBusy = nameof(URIActivationFailedBusy);
    public static readonly string LoadingPageHeaderLocalText = nameof(LoadingPageHeaderLocalText);
    public static readonly string LoadingPageHeaderTargetText = nameof(LoadingPageHeaderTargetText);
    public static readonly string LoadingPageSetupTargetText = nameof(LoadingPageSetupTargetText);
    public static readonly string LoadingTasksTitleText = nameof(LoadingTasksTitleText);
    public static readonly string LoadingLogsTitleText = nameof(LoadingLogsTitleText);
    public static readonly string LoadingExecutingProgress = nameof(LoadingExecutingProgress);
    public static readonly string LoadingExecutingProgressForTarget = nameof(LoadingExecutingProgressForTarget);
    public static readonly string ActionCenterDisplay = nameof(ActionCenterDisplay);
    public static readonly string NeedsRebootMessage = nameof(NeedsRebootMessage);
    public static readonly string ApplicationsPageTitle = nameof(ApplicationsPageTitle);
    public static readonly string ReposConfigPageTitle = nameof(ReposConfigPageTitle);
    public static readonly string ReviewPageTitle = nameof(ReviewPageTitle);
    public static readonly string SummaryPageOneApplicationInstalled = nameof(SummaryPageOneApplicationInstalled);
    public static readonly string SummaryPageOneRepositoryCloned = nameof(SummaryPageOneRepositoryCloned);
    public static readonly string SummaryPageAppsDownloadedCount = nameof(SummaryPageAppsDownloadedCount);
    public static readonly string SummaryPageReposClonedCount = nameof(SummaryPageReposClonedCount);
    public static readonly string SummaryConfigurationErrorsCountText = nameof(SummaryConfigurationErrorsCountText);
    public static readonly string SummaryPageTargetMachineFailedTaskText = nameof(SummaryPageTargetMachineFailedTaskText);
    public static readonly string SSHConnectionStringNotAllowed = nameof(SSHConnectionStringNotAllowed);
    public static readonly string NoInternetConnectionTitle = nameof(NoInternetConnectionTitle);
    public static readonly string NoInternetConnectionDescription = nameof(NoInternetConnectionDescription);

    // Repository loading screen messages
    public static readonly string CloneRepoCreating = nameof(CloneRepoCreating);
    public static readonly string CloneRepoCreated = nameof(CloneRepoCreated);
    public static readonly string CloneRepoError = nameof(CloneRepoError);
    public static readonly string CloneRepoErrorForActionCenter = nameof(CloneRepoErrorForActionCenter);
    public static readonly string CloneRepoRestart = nameof(CloneRepoRestart);

    // Repository Next Steps messages
    public static readonly string CloneRepoNextStepsView = nameof(CloneRepoNextStepsView);
    public static readonly string CloneRepoNextStepsRun = nameof(CloneRepoNextStepsRun);
    public static readonly string CloneRepoNextStepsFileFound = nameof(CloneRepoNextStepsFileFound);
    public static readonly string CloneRepoNextStepsDescription = nameof(CloneRepoNextStepsDescription);

    // Configure task loading screen messages
    public static readonly string ApplyingConfigurationMessage = nameof(ApplyingConfigurationMessage);
    public static readonly string ConfigureTaskCreating = nameof(ConfigureTaskCreating);
    public static readonly string ConfigureTaskCreated = nameof(ConfigureTaskCreated);
    public static readonly string ConfigureTaskError = nameof(ConfigureTaskError);
    public static readonly string ConfigureTaskRestart = nameof(ConfigureTaskRestart);

    // App download loading screen messages
    public static readonly string StartingInstallPackageMessage = nameof(StartingInstallPackageMessage);
    public static readonly string DownloadAppCreating = nameof(DownloadAppCreating);
    public static readonly string DownloadAppCreated = nameof(DownloadAppCreated);
    public static readonly string DownloadAppError = nameof(DownloadAppError);
    public static readonly string DownloadAppRestart = nameof(DownloadAppRestart);

    // Dev drive loading screen messages
    public static readonly string DevDriveNotAdminError = nameof(DevDriveNotAdminError);
    public static readonly string DevDriveCreating = nameof(DevDriveCreating);
    public static readonly string DevDriveCreated = nameof(DevDriveCreated);
    public static readonly string DevDriveErrorWithReason = nameof(DevDriveErrorWithReason);
    public static readonly string DevDriveRestart = nameof(DevDriveRestart);

    // Loading screen
    public static readonly string LoadingScreenActionCenterErrors = nameof(LoadingScreenActionCenterErrors);
    public static readonly string LoadingPageSteps = nameof(LoadingPageSteps);
    public static readonly string LoadingScreenGoToSummaryButtonContent = nameof(LoadingScreenGoToSummaryButtonContent);

    // Repo tool
    public static readonly string RepoToolNextButtonTooltip = nameof(RepoToolNextButtonTooltip);
    public static readonly string RepoToolNoRepositoriesMessage = nameof(RepoToolNoRepositoriesMessage);
    public static readonly string RepoAccountPagePrimaryButtonText = nameof(RepoAccountPagePrimaryButtonText);
    public static readonly string RepoEverythingElsePrimaryButtonText = nameof(RepoEverythingElsePrimaryButtonText);
    public static readonly string RepoPageEditClonePathAutomationProperties = nameof(RepoPageEditClonePathAutomationProperties);
    public static readonly string RepoPageRemoveRepoAutomationProperties = nameof(RepoPageRemoveRepoAutomationProperties);
    public static readonly string ClonePathNotFullyQualifiedMessage = nameof(ClonePathNotFullyQualifiedMessage);
    public static readonly string ClonePathNotFolder = nameof(ClonePathNotFolder);
    public static readonly string ClonePathDriveDoesNotExist = nameof(ClonePathDriveDoesNotExist);
    public static readonly string RepoToolAddAnotherAccount = nameof(RepoToolAddAnotherAccount);
    public static readonly string SetupShellRepoConfigLocalMachine = nameof(SetupShellRepoConfigLocalMachine);
    public static readonly string SetupShellRepoConfigTargetMachine = nameof(SetupShellRepoConfigTargetMachine);

    // Url Validation
    public static readonly string UrlValidationBadUrl = nameof(UrlValidationBadUrl);
    public static readonly string UrlValidationNotFound = nameof(UrlValidationNotFound);
    public static readonly string UrlValidationRepoAlreadyAdded = nameof(UrlValidationRepoAlreadyAdded);
    public static readonly string UrlNoAccountsHaveAccess = nameof(UrlNoAccountsHaveAccess);
    public static readonly string UrlCancelButtonText = nameof(UrlCancelButtonText);

    // Install errors
    public static readonly string InstallPackageError = nameof(InstallPackageError);
    public static readonly string InstallPackageErrorMessagePackageInUse = nameof(InstallPackageErrorMessagePackageInUse);
    public static readonly string InstallPackageErrorMessageInstallInProgress = nameof(InstallPackageErrorMessageInstallInProgress);
    public static readonly string InstallPackageErrorMessageFileInUse = nameof(InstallPackageErrorMessageFileInUse);
    public static readonly string InstallPackageErrorMessageMissingDependency = nameof(InstallPackageErrorMessageMissingDependency);
    public static readonly string InstallPackageErrorMessageDiskFull = nameof(InstallPackageErrorMessageDiskFull);
    public static readonly string InstallPackageErrorMessageInsufficientMemory = nameof(InstallPackageErrorMessageInsufficientMemory);
    public static readonly string InstallPackageErrorMessageNoNetwork = nameof(InstallPackageErrorMessageNoNetwork);
    public static readonly string InstallPackageErrorMessageContactSupport = nameof(InstallPackageErrorMessageContactSupport);
    public static readonly string InstallPackageErrorMessageRebootRequiredToFinish = nameof(InstallPackageErrorMessageRebootRequiredToFinish);
    public static readonly string InstallPackageErrorMessageRebootRequiredToInstall = nameof(InstallPackageErrorMessageRebootRequiredToInstall);
    public static readonly string InstallPackageErrorMessageRebootInitiated = nameof(InstallPackageErrorMessageRebootInitiated);
    public static readonly string InstallPackageErrorMessageCancelledByUser = nameof(InstallPackageErrorMessageCancelledByUser);
    public static readonly string InstallPackageErrorMessageAlreadyInstalled = nameof(InstallPackageErrorMessageAlreadyInstalled);
    public static readonly string InstallPackageErrorMessageDowngrade = nameof(InstallPackageErrorMessageDowngrade);
    public static readonly string InstallPackageErrorMessageBlockedByPolicy = nameof(InstallPackageErrorMessageBlockedByPolicy);
    public static readonly string InstallPackageErrorMessageDependencies = nameof(InstallPackageErrorMessageDependencies);
    public static readonly string InstallPackageErrorMessagePackageInUseByApplication = nameof(InstallPackageErrorMessagePackageInUseByApplication);
    public static readonly string InstallPackageErrorMessageInvalidParameter = nameof(InstallPackageErrorMessageInvalidParameter);
    public static readonly string InstallPackageErrorMessageSystemNotSupported = nameof(InstallPackageErrorMessageSystemNotSupported);
    public static readonly string InstallPackageErrorMessageSystemMessage = nameof(InstallPackageErrorMessageSystemMessage);
    public static readonly string InstallPackageErrorBlockedByPolicy = nameof(InstallPackageErrorBlockedByPolicy);
    public static readonly string InstallPackageErrorDownloadError = nameof(InstallPackageErrorDownloadError);
    public static readonly string InstallPackageErrorInternalError = nameof(InstallPackageErrorInternalError);
    public static readonly string InstallPackageErrorNoApplicableInstallers = nameof(InstallPackageErrorNoApplicableInstallers);
    public static readonly string InstallPackageErrorUnknownErrorWithErrorCode = nameof(InstallPackageErrorUnknownErrorWithErrorCode);
    public static readonly string InstallPackageErrorUnknownErrorWithErrorCodeAndExitCode = nameof(InstallPackageErrorUnknownErrorWithErrorCodeAndExitCode);

    // WinGet Configuration
    public static readonly string ConfigurationFieldInvalidType = nameof(ConfigurationFieldInvalidType);
    public static readonly string ConfigurationFieldInvalidValue = nameof(ConfigurationFieldInvalidValue);
    public static readonly string ConfigurationFieldMissing = nameof(ConfigurationFieldMissing);
    public static readonly string ConfigurationFileInvalid = nameof(ConfigurationFileInvalid);
    public static readonly string ConfigurationFileOpenUnknownError = nameof(ConfigurationFileOpenUnknownError);
    public static readonly string ConfigurationFileVersionUnknown = nameof(ConfigurationFileVersionUnknown);
    public static readonly string ConfigurationUnitHasDuplicateIdentifier = nameof(ConfigurationUnitHasDuplicateIdentifier);
    public static readonly string ConfigurationUnitHasMissingDependency = nameof(ConfigurationUnitHasMissingDependency);
    public static readonly string ConfigurationUnitAssertHadNegativeResult = nameof(ConfigurationUnitAssertHadNegativeResult);
    public static readonly string ConfigurationUnitNotFoundInModule = nameof(ConfigurationUnitNotFoundInModule);
    public static readonly string ConfigurationUnitNotFound = nameof(ConfigurationUnitNotFound);
    public static readonly string ConfigurationUnitMultipleMatches = nameof(ConfigurationUnitMultipleMatches);
    public static readonly string ConfigurationUnitFailedDuringGet = nameof(ConfigurationUnitFailedDuringGet);
    public static readonly string ConfigurationUnitFailedDuringTest = nameof(ConfigurationUnitFailedDuringTest);
    public static readonly string ConfigurationUnitFailedDuringSet = nameof(ConfigurationUnitFailedDuringSet);
    public static readonly string ConfigurationUnitModuleConflict = nameof(ConfigurationUnitModuleConflict);
    public static readonly string ConfigurationUnitModuleImportFailed = nameof(ConfigurationUnitModuleImportFailed);
    public static readonly string ConfigurationUnitReturnedInvalidResult = nameof(ConfigurationUnitReturnedInvalidResult);
    public static readonly string ConfigurationUnitManuallySkipped = nameof(ConfigurationUnitManuallySkipped);
    public static readonly string ConfigurationUnitNotRunDueToDependency = nameof(ConfigurationUnitNotRunDueToDependency);
    public static readonly string WinGetConfigUnitSettingConfigRoot = nameof(WinGetConfigUnitSettingConfigRoot);
    public static readonly string WinGetConfigUnitImportModuleAdmin = nameof(WinGetConfigUnitImportModuleAdmin);
    public static readonly string ConfigurationUnitFailedConfigSet = nameof(ConfigurationUnitFailedConfigSet);
    public static readonly string ConfigurationUnitFailedInternal = nameof(ConfigurationUnitFailedInternal);
    public static readonly string ConfigurationUnitFailedPrecondition = nameof(ConfigurationUnitFailedPrecondition);
    public static readonly string ConfigurationUnitFailedSystemState = nameof(ConfigurationUnitFailedSystemState);
    public static readonly string ConfigurationUnitFailedUnitProcessing = nameof(ConfigurationUnitFailedUnitProcessing);
    public static readonly string ConfigurationUnitNotRunDueToFailedAssert = nameof(ConfigurationUnitNotRunDueToFailedAssert);

    // Setup target flow
    public static readonly string SetupTargetPageTitle = nameof(SetupTargetPageTitle);
    public static readonly string SetupTargetAllComboBoxOption = nameof(SetupTargetAllComboBoxOption);
    public static readonly string SetupTargetConfigurationUnknown = nameof(SetupTargetConfigurationUnknown);
    public static readonly string SetupTargetConfigurationPending = nameof(SetupTargetConfigurationPending);
    public static readonly string SetupTargetConfigurationInProgress = nameof(SetupTargetConfigurationInProgress);
    public static readonly string SetupTargetConfigurationCompleted = nameof(SetupTargetConfigurationCompleted);
    public static readonly string SetupTargetConfigurationShuttingDownDevice = nameof(SetupTargetConfigurationShuttingDownDevice);
    public static readonly string SetupTargetConfigurationStartingDevice = nameof(SetupTargetConfigurationStartingDevice);
    public static readonly string SetupTargetConfigurationRestartingDevice = nameof(SetupTargetConfigurationRestartingDevice);
    public static readonly string SetupTargetConfigurationProvisioningDevice = nameof(SetupTargetConfigurationProvisioningDevice);
    public static readonly string SetupTargetConfigurationWaitingForAdminUserLogon = nameof(SetupTargetConfigurationWaitingForAdminUserLogon);
    public static readonly string SetupTargetConfigurationWaitingForUserLogon = nameof(SetupTargetConfigurationWaitingForUserLogon);
    public static readonly string SetupTargetConfigurationSkipped = nameof(SetupTargetConfigurationSkipped);
    public static readonly string SetupTargetConfigurationOpenConfigFailed = nameof(SetupTargetConfigurationOpenConfigFailed);
    public static readonly string SetupTargetConfigurationUnitProgressMessage = nameof(SetupTargetConfigurationUnitProgressMessage);
    public static readonly string SetupTargetConfigurationSetProgressMessage = nameof(SetupTargetConfigurationSetProgressMessage);
    public static readonly string SetupTargetConfigurationUnitProgressError = nameof(SetupTargetConfigurationUnitProgressError);
    public static readonly string ConfigureTargetApplyConfigurationStopped = nameof(ConfigureTargetApplyConfigurationStopped);
    public static readonly string ConfigureTargetApplyConfigurationStoppedWithNoEndingMessage = nameof(ConfigureTargetApplyConfigurationStoppedWithNoEndingMessage);
    public static readonly string ConfigureTargetApplyConfigurationActionNeeded = nameof(ConfigureTargetApplyConfigurationActionNeeded);
    public static readonly string SetupTargetExtensionApplyingConfiguration = nameof(SetupTargetExtensionApplyingConfiguration);
    public static readonly string SetupTargetExtensionApplyingConfigurationActionRequired = nameof(SetupTargetExtensionApplyingConfigurationActionRequired);
    public static readonly string SetupTargetExtensionApplyConfigurationError = nameof(SetupTargetExtensionApplyConfigurationError);
    public static readonly string SetupTargetExtensionApplyConfigurationSuccess = nameof(SetupTargetExtensionApplyConfigurationSuccess);
    public static readonly string SetupTargetExtensionApplyConfigurationRebootRequired = nameof(SetupTargetExtensionApplyConfigurationRebootRequired);
    public static readonly string SetupTargetMachineName = nameof(SetupTargetMachineName);
    public static readonly string ConfigureTargetApplyConfigurationActionFailureRetry = nameof(ConfigureTargetApplyConfigurationActionFailureRetry);
    public static readonly string ConfigureTargetApplyConfigurationActionFailureEnd = nameof(ConfigureTargetApplyConfigurationActionFailureEnd);
    public static readonly string ConfigureTargetApplyConfigurationActionSuccess = nameof(ConfigureTargetApplyConfigurationActionSuccess);
    public static readonly string SetupTargetReviewPageDefaultInfoBarTitle = nameof(SetupTargetReviewPageDefaultInfoBarTitle);
    public static readonly string SetupTargetReviewPageDefaultInfoBarMessage = nameof(SetupTargetReviewPageDefaultInfoBarMessage);
    public static readonly string SetupTargetReviewPageHyperVInfoBarMessage = nameof(SetupTargetReviewPageHyperVInfoBarMessage);
    public static readonly string SetupTargetReviewTabTitle = nameof(SetupTargetReviewTabTitle);
    public static readonly string SetupTargetUnknownStatus = nameof(SetupTargetUnknownStatus);
    public static readonly string SetupTargetSortAToZLabel = nameof(SetupTargetSortAToZLabel);
    public static readonly string SetupTargetSortZToALabel = nameof(SetupTargetSortZToALabel);
    public static readonly string SetupTargetPageSyncButton = nameof(SetupTargetPageSyncButton);
    public static readonly string SetupTargetConfigurationUnitCompleted = nameof(SetupTargetConfigurationUnitCompleted);
    public static readonly string SetupTargetConfigurationUnitInProgress = nameof(SetupTargetConfigurationUnitInProgress);
    public static readonly string SetupTargetConfigurationUnitPending = nameof(SetupTargetConfigurationUnitPending);
    public static readonly string SetupTargetConfigurationUnitSkipped = nameof(SetupTargetConfigurationUnitSkipped);
    public static readonly string SetupTargetConfigurationSetCurrentState = nameof(SetupTargetConfigurationSetCurrentState);
    public static readonly string SetupTargetConfigurationUnitCurrentState = nameof(SetupTargetConfigurationUnitCurrentState);
    public static readonly string SetupTargetConfigurationProgressUpdate = nameof(SetupTargetConfigurationProgressUpdate);
    public static readonly string SetupTargetConfigurationUnitProgressErrorWithMsg = nameof(SetupTargetConfigurationUnitProgressErrorWithMsg);
    public static readonly string SetupTargetConfigurationUnitUnknown = nameof(SetupTargetConfigurationUnitUnknown);
    public static readonly string NoEnvironmentsButExtensionsInstalledButton = nameof(NoEnvironmentsButExtensionsInstalledButton);
    public static readonly string NoEnvironmentsButExtensionsInstalledCallToAction = nameof(NoEnvironmentsButExtensionsInstalledCallToAction);
    public static readonly string NoEnvironmentsAndExtensionsNotInstalledCallToAction = nameof(NoEnvironmentsAndExtensionsNotInstalledCallToAction);
    public static readonly string NoEnvironmentsAndExtensionsNotInstalledButton = nameof(NoEnvironmentsAndExtensionsNotInstalledButton);

    // Quickstart Playground
    public static readonly string QuickstartPlaygroundLaunchButton = nameof(QuickstartPlaygroundLaunchButton);

    // Create Environment flow
    public static readonly string SelectEnvironmentPageTitle = nameof(SelectEnvironmentPageTitle);
    public static readonly string ConfigureEnvironmentPageTitle = nameof(ConfigureEnvironmentPageTitle);
    public static readonly string EnvironmentCreationReviewPageTitle = nameof(EnvironmentCreationReviewPageTitle);
    public static readonly string EnvironmentCreationReviewTabTitle = nameof(EnvironmentCreationReviewTabTitle);
    public static readonly string EnvironmentCreationError = nameof(EnvironmentCreationError);
    public static readonly string StartingEnvironmentCreation = nameof(StartingEnvironmentCreation);
    public static readonly string EnvironmentCreationOperationInitializationFinished = nameof(EnvironmentCreationOperationInitializationFinished);
    public static readonly string EnvironmentCreationForProviderStarted = nameof(EnvironmentCreationForProviderStarted);
    public static readonly string EnvironmentCreationFailedToGetProviderInformation = nameof(EnvironmentCreationFailedToGetProviderInformation);
    public static readonly string EnvironmentCreationReviewExpanderDescription = nameof(EnvironmentCreationReviewExpanderDescription);
    public static readonly string CreateEnvironmentButtonText = nameof(CreateEnvironmentButtonText);
    public static readonly string SetupShellReviewPageDescriptionForEnvironmentCreation = nameof(SetupShellReviewPageDescriptionForEnvironmentCreation);
    public static readonly string EnvironmentCreationUILoadingMessage = nameof(EnvironmentCreationUILoadingMessage);

    // Summary page
    public static readonly string SummaryPageOpenDashboard = nameof(SummaryPageOpenDashboard);
    public static readonly string SummaryPageRedirectToEnvironmentPageButton = nameof(SummaryPageRedirectToEnvironmentPageButton);
    public static readonly string SummaryPageHeader = nameof(SummaryPageHeader);
    public static readonly string SummaryPageHeaderForEnvironmentCreationText = nameof(SummaryPageHeaderForEnvironmentCreationText);

    // Review page
    public static readonly string ReviewExpanderDescription = nameof(ReviewExpanderDescription);
    public static readonly string SetupShellReviewPageDescription = nameof(SetupShellReviewPageDescription);
}
