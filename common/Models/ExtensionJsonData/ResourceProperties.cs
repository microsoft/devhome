// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;

namespace DevHome.Common.Models.ExtensionJsonData;

public class ResourceProperties
{
    private readonly StringResource _stringResource = new("DevHome.Common.pri", "DevHome.Common/Resources");

    public string DisplayNameKey { get; set; } = string.Empty;

    public string PublisherDisplayNameKey { get; set; } = string.Empty;

    public string ShortDescriptionKey { get; set; } = string.Empty;

    public string LongDescriptionKey { get; set; } = string.Empty;

    public string LocalizedDisplayName => _stringResource.GetLocalized(DisplayNameKey);

    public string LocalizedPublisherDisplayName => _stringResource.GetLocalized(PublisherDisplayNameKey);

    public string LocalizedShortDescription => _stringResource.GetLocalized(ShortDescriptionKey);

    public string LocalizedLongDescription => _stringResource.GetLocalized(LongDescriptionKey);
}
