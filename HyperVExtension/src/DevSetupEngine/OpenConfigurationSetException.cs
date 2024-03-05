// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Management.Configuration;

namespace HyperVExtension.DevSetupEngine;

public class OpenConfigurationSetException : Exception
{
    // Open configuration error codes:
    // Reference: https://github.com/microsoft/winget-cli/blob/master/src/AppInstallerSharedLib/Public/AppInstallerErrors.h
    public const int WingetConfigErrorInvalidConfigurationFile = unchecked((int)0x8A15C001);
    public const int WingetConfigErrorInvalidYaml = unchecked((int)0x8A15C002);
    public const int WingetConfigErrorInvalidField = unchecked((int)0x8A15C003);
    public const int WingetConfigErrorUnknownConfigurationFileVersion = unchecked((int)0x8A15C004);

    public OpenConfigurationSetResult OpenConfigurationSetResult { get; }

    public OpenConfigurationSetException(OpenConfigurationSetResult openConfigurationSetResult)
    {
        OpenConfigurationSetResult = openConfigurationSetResult;
    }
}
