// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Common.Services;

public interface IAppInfoService
{
    /// <summary>
    /// Gets the localized name of the application.
    /// </summary>
    /// <returns>The localized name of the application.</returns>
    public string GetAppNameLocalized();

    /// <summary>
    /// Gets the version of the application.
    /// </summary>
    /// <returns>The version of the application.</returns>
    public Version GetAppVersion();

    /// <summary>
    /// Gets the path for the icon of the application.
    /// </summary>
    public string IconPath { get; }

    /// <summary>
    /// Gets the preferred language of the user.
    /// If no preferred language is set, then default to <see cref="CultureInfo.CurrentCulture"/>
    /// </summary>
    /// <remarks>Preferred language is set in the system settings under "Language & region"</remarks>
    public string UserPreferredLanguage { get; }
}
