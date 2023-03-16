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
    public static readonly string Close = nameof(Close);
    public static readonly string ConfigurationFileTypeNotSupported = nameof(ConfigurationFileTypeNotSupported);
    public static readonly string DevDriveWindowByteUnitComboBoxGB = nameof(DevDriveWindowByteUnitComboBoxGB);
    public static readonly string DevDriveWindowByteUnitComboBoxTB = nameof(DevDriveWindowByteUnitComboBoxTB);
    public static readonly string FileTypeNotSupported = nameof(FileTypeNotSupported);
    public static readonly string Next = nameof(Next);
    public static readonly string NoSearchResultsFoundTitle = nameof(NoSearchResultsFoundTitle);
    public static readonly string PackagesCount = nameof(PackagesCount);
    public static readonly string ResultCount = nameof(ResultCount);
    public static readonly string Repository = nameof(Repository);
    public static readonly string SelectedPackagesCount = nameof(SelectedPackagesCount);
    public static readonly string SetUpButton = nameof(SetUpButton);
    public static readonly string ViewConfiguration = nameof(ViewConfiguration);
    public static readonly string LoadingExecutingProgress = nameof(LoadingExecutingProgress);
    public static readonly string ActionCenterDisplay = nameof(ActionCenterDisplay);

    // Action center and loading screen
    public static readonly string LoadingScreenCloningRepositoryMainText = nameof(LoadingScreenCloningRepositoryMainText);
    public static readonly string LoadingScreenCloningRepositoryFinished = nameof(LoadingScreenCloningRepositoryFinished);
    public static readonly string LoadingScreenCloningRepositoryErrorText = nameof(LoadingScreenCloningRepositoryErrorText);
    public static readonly string LoadingScreenCloningReposityNeedsAttention = nameof(LoadingScreenCloningReposityNeedsAttention);
    public static readonly string LoadingScreenCloningRepositoryErrorTestSecondary = nameof(LoadingScreenCloningRepositoryErrorTestSecondary);
    public static readonly string LoadingScreenCloningRepositoryErrorMainButtonContent = nameof(LoadingScreenCloningRepositoryErrorMainButtonContent);
    public static readonly string LoadingScreenCloningRepositoryErrorSecondaryButtonContent = nameof(LoadingScreenCloningRepositoryErrorSecondaryButtonContent);
}
