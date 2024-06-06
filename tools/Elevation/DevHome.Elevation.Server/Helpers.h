// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <filesystem>
#include <span>

#include <wrl/implements.h>

#include <wil/com.h>
#include <wil/resource.h>

#include "DevHome.Elevation.h"

template<typename T, typename I>
HRESULT MakeAndInitializeToInterface(_COM_Outptr_ I** result) noexcept try
{
    wil::com_ptr<T> zone;
    THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<T>(&zone));
    auto intf = zone.query<I>();
    *result = intf.detach();
    return S_OK;
}
CATCH_RETURN()

HRESULT MakeElevationZone_ElevationZoneA(_COM_Outptr_ ABI::DevHome::Elevation::IElevationZone** result) noexcept;
HRESULT MakeElevationZone_VirtualMachineManagement(_COM_Outptr_ ABI::DevHome::Elevation::IElevationZone** result) noexcept;

inline HRESULT MakeElevationZone(ABI::DevHome::Elevation::ElevationZone elevationZone, _COM_Outptr_ ABI::DevHome::Elevation::IElevationZone** result) noexcept
{
    using namespace ABI::DevHome::Elevation;
    if (elevationZone == ElevationZone::ElevationZoneA)
    {
        return MakeElevationZone_ElevationZoneA(result);
    }
    else if (elevationZone == ElevationZone::ElevationZoneB)
    {
        return E_NOTIMPL;
    }
    else if (elevationZone == ElevationZone::VirtualMachineManagement)
    {
        return MakeElevationZone_VirtualMachineManagement(result);
    }
    return E_NOTIMPL;
}
