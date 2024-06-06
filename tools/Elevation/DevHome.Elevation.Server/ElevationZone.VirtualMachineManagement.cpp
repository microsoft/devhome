// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <wrl/client.h>
#include <wrl/implements.h>
#include <wrl/module.h>

#include <wil/registry.h>
#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include <windows.h>

#include "Helpers.h"
#include "Utility.h"
#include "DevHome.Elevation.h"

HRESULT MakeElevationZone_VirtualMachineManagement(_COM_Outptr_ ABI::DevHome::Elevation::IElevationZone** result) noexcept
{
    return MakeAndInitializeToInterface<ABI::DevHome::Elevation::Zones::VirtualMachineManagement, ABI::DevHome::Elevation::IElevationZone>(result);
}

static bool GetWindowsOptionalFeatureEnabled(std::wstring const& feature)
{
    auto commandString = std::wstring{} + L"-ExecutionPolicy Bypass -Command \"(Get-WindowsOptionalFeature -Online -FeatureName " + feature + L").State -eq Enabled\"";

    // Run powershell script with CreateProcessW
    THROW_IF_WIN32_BOOL_FALSE(CreateProcessW(L"powershell.exe", (LPWSTR)commandString.c_str(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, nullptr, nullptr));

    // TODO: Get exit code
    DWORD exitCode = ERROR_SUCCESS;
    
    return exitCode == ERROR_SUCCESS;
}

static bool EnableWindowsOptionalFeature(std::wstring const& feature)
{
    auto commandString = std::wstring{} + L"-ExecutionPolicy Bypass -Command \"Enable-WindowsOptionalFeature -Online -NoRestart -All -FeatureName " + feature + L"\"";

    // Run powershell script with CreateProcessW
    THROW_IF_WIN32_BOOL_FALSE(CreateProcessW(L"powershell.exe", (LPWSTR)commandString.c_str(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, nullptr, nullptr));

    // TODO: Get exit code
    DWORD exitCode = ERROR_SUCCESS;

    return exitCode == ERROR_SUCCESS;
}

static bool DisableWindowsOptionalFeature(std::wstring const& feature)
{
    auto commandString = std::wstring{} + L"-ExecutionPolicy Bypass -Command \"Disable-WindowsOptionalFeature -Online -NoRestart -All -FeatureName " + feature + L"\"";

    // Run powershell script with CreateProcessW
    THROW_IF_WIN32_BOOL_FALSE(CreateProcessW(L"powershell.exe", (LPWSTR)commandString.c_str(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, nullptr, nullptr));

    // TODO: Get exit code
    DWORD exitCode = ERROR_SUCCESS;

    return exitCode == ERROR_SUCCESS;
}

static bool SetWindowsOptionalFeature(std::wstring const& feature, bool doEnable)
{
    if (doEnable)
    {
        return EnableWindowsOptionalFeature(feature);
    }
    else
    {
        return DisableWindowsOptionalFeature(feature);
    }
}

namespace ABI::DevHome::Elevation::Zones
{
    class VirtualMachineManagement :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IVirtualMachineManagement,
            IElevationZone,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_Elevation_Zones_VirtualMachineManagement, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize() noexcept
        {
            return S_OK;
        }

        STDMETHODIMP ModifyFeatures(
            /* [in] */ unsigned int featureStringsCount,
            /* [size_is(featuresStringLength), in] */ HSTRING* featureStrings,
            /* [out, retval] */ ABI::DevHome::Elevation::Zones::ModifyFeaturesStatus* result
        ) noexcept
        try
        {
            auto fullStatus = ModifyFeaturesStatus_NoChange;

            // Foreach feature in featuresString
            for (unsigned int i = 0; i < featureStringsCount; i++)
            {
                // TODO: try catch each feature

                // TODO: Sanitize 'feature' input string here (treat input as untrusted).

                // TODO: Split string on "=" to get enable/disable
                std::wstring action = L"Enable";

                bool doEnable = (action == L"Enable");

                // Get feature string
                std::wstring feature = WindowsGetStringRawBuffer(featureStrings[i], nullptr);

                if (doEnable != GetWindowsOptionalFeatureEnabled(feature))
                {
                    auto status = ModifyFeaturesStatus_Failure;
                    if (SetWindowsOptionalFeature(feature, doEnable))
                    {
                        status = ModifyFeaturesStatus_Success;
                    }

                    if (status == ModifyFeaturesStatus_Failure || fullStatus == ModifyFeaturesStatus_NoChange)
                    {
                        fullStatus = status;
                    }
                }
            }

            *result = fullStatus;
            return S_OK;
        }
        CATCH_RETURN()
    };
}