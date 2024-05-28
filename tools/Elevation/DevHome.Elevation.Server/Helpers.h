// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <filesystem>
#include <span>

#include <wrl/implements.h>

#include <wil/com.h>
#include <wil/resource.h>

#include "DevHome.Elevation.h"
//#include "PerformanceRecorderEngine.h"

struct com_ptr_deleter
{
    template<typename T>
    void operator()(_Pre_opt_valid_ _Frees_ptr_opt_ T p) const
    {
        if (p)
        {
            p.reset();
        }
    }
};

template<typename T, typename ArrayDeleter = wil::process_heap_deleter>
using unique_comptr_array = wil::unique_any_array_ptr<typename wil::com_ptr_nothrow<T>, ArrayDeleter, com_ptr_deleter>;

template<typename T>
unique_comptr_array<T> make_unique_comptr_array(size_t numOfElements)
{
    auto list = unique_comptr_array<T>(reinterpret_cast<wil::com_ptr_nothrow<T>*>(HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, numOfElements * sizeof(wil::com_ptr_nothrow<T>))), numOfElements);
    THROW_IF_NULL_ALLOC(list.get());
    return list;
}

// Create a performance recorder engine
//wil::com_ptr<ABI::DevHome::QuietBackgroundProcesses::IPerformanceRecorderEngine> MakePerformanceRecorderEngine();

// Read/write the performance data to/from disk
//void WritePerformanceDataToDisk(_In_ PCWSTR path, const std::span<ProcessPerformanceSummary>& data);
//std::vector<ProcessPerformanceSummary> ReadPerformanceDataFromDisk(_In_ PCWSTR path);\


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
