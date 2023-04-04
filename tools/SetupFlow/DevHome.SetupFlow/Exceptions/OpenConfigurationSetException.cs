// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.Exceptions;

public class OpenConfigurationSetException : Exception
{
    // Open configuration error codes:
    // Reference: https://github.com/microsoft/winget-cli/blob/master/src/AppInstallerSharedLib/Public/AppInstallerErrors.h
    public const int WingetConfigErrorInvalidConfigurationFile = unchecked((int)0x8A15C001);
    public const int WingetConfigErrorInvalidYaml = unchecked((int)0x8A15C002);
    public const int WingetConfigErrorInvalidField = unchecked((int)0x8A15C003);
    public const int WingetConfigErrorUnknownConfigurationFileVersion = unchecked((int)0x8A15C004);

    /// <summary>
    /// Gets the <see cref="OpenConfigurationSetResult.ResultCode"/>
    /// </summary>
    public Exception ResultCode { get; }

    /// <summary>
    /// Gets the <see cref="OpenConfigurationSetResult.Field"/>
    /// </summary>
    public string Field { get; }

    public OpenConfigurationSetException(Exception resultCode, string field)
    {
        ResultCode = resultCode;
        Field = field;
    }
}
