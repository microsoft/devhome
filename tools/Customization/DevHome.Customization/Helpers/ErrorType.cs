// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Customization.Helpers;

public enum ErrorType
{
    None,
    Unknown,
    RepositoryProviderCreationFailed,
    OpenRepositoryFailed,
    SourceControlExtensionValidationFailed,
    RegistrationWithFileExplorerFailed,
}
