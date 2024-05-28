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

#include "Helpers.h"
#include "Utility.h"
#include "DevHome.Elevation.h"

HRESULT MakeElevationZone_ElevationZoneA(_COM_Outptr_ ABI::DevHome::Elevation::IElevationZone** result) noexcept
{
    return MakeAndInitializeToInterface<ABI::DevHome::Elevation::Zones::ElevationZoneA, ABI::DevHome::Elevation::IElevationZone>(result);
}

namespace ABI::DevHome::Elevation::Zones
{
    class ElevationZoneA :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IElevationZoneA,
            IElevationZone,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_Elevation_Zones_ElevationZoneA, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize() noexcept
        {
            return S_OK;
        }

        STDMETHODIMP GetSomething(_Out_ unsigned int* result) noexcept
        {
            // try get registry key to hklm/software/microsoft/windows/currentversion/devhome/quietbackgroundprocesses
            if (auto durationOverride = try_get_registry_value_dword(HKEY_LOCAL_MACHINE, LR"(Software\Microsoft\Windows\CurrentVersion\DevHome\QuietBackgroundProcesses)", L"Duration"))
            {
                //*result = (unsigned int)std::chrono::seconds(durationOverride.value()).count();
            }
            *result = 1111;
            return S_OK;
        }
    };
}