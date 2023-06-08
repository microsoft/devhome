// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Services;

namespace DevHome.SetupFlow.Services;
public interface ISetupFlowStringResource : IStringResource
{
    /// <summary>
    /// Gets the localized system error message from the HResult passed into
    /// the function.
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="logComponent">Component string used for tagging log messages with the appropriate caller domain</param>
    /// <returns>
    /// Localized string error message from Hresult if exists on the system else just the error code in Hexidecimal format
    /// </returns>
    public string GetLocalizedErrorMsg(int errorCode, string logComponent);
}
