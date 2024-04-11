// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <chrono>
#include <memory>
#include <mutex>

#include <wrl/client.h>
#include <wrl/implements.h>
#include <wrl/module.h>

#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include <Windows.Foundation.h>
#include <Windows.Foundation.Collections.h>

#include "TimedQuietSession.h"
#include "DevHome.QuietBackgroundProcesses.h"
#include "PerformanceRecorderEngine.h"


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

namespace ABI::DevHome::QuietBackgroundProcesses
{
    class ProcessRow :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IProcessRow,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_QuietBackgroundProcesses_ProcessRow, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize(ProcessPerformanceSummary summary) noexcept
        {
            m_summary = summary;
            return S_OK;
        }

        STDMETHODIMP get_Pid(unsigned int* value) noexcept override
        try
        {
            *value = m_summary.pid;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_Name(HSTRING* value) noexcept override
        try
        {
            Microsoft::WRL::Wrappers::HString str;
            str.Set(m_summary.name);
            *value = str.Detach();
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_PackageFullName(HSTRING* value) noexcept override
        try
        {
            Microsoft::WRL::Wrappers::HString str;
            str.Set(m_summary.packageFullName);
            *value = str.Detach();
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_Aumid(HSTRING* value) noexcept override
        try
        {
            Microsoft::WRL::Wrappers::HString str;
            str.Set(m_summary.aumid);
            *value = str.Detach();
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_Path(HSTRING* value) noexcept override
        try
        {
            Microsoft::WRL::Wrappers::HString str;
            str.Set(m_summary.path);
            *value = str.Detach();
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_Category(ABI::DevHome::QuietBackgroundProcesses::ProcessCategory* value) noexcept override
        try
        {
            *value = static_cast<ABI::DevHome::QuietBackgroundProcesses::ProcessCategory>(m_summary.category);
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_CreateTime(struct ABI::Windows::Foundation::DateTime* value) noexcept override
        try
        {
            INT64 time = m_summary.createTime.dwLowDateTime + ((UINT64)m_summary.createTime.dwHighDateTime << 32);
            *value = ABI::Windows::Foundation::DateTime{ time };
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_ExitTime(struct ABI::Windows::Foundation::DateTime* value) noexcept override
        try
        {
            INT64 time = m_summary.exitTime.dwLowDateTime + ((UINT64)m_summary.exitTime.dwHighDateTime << 32);
            *value = ABI::Windows::Foundation::DateTime{ time };
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_SampleCount(unsigned __int64* value) noexcept override
        try
        {
            *value = m_summary.sampleCount;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_PercentCumulative(double* value) noexcept override
        try
        {
            *value = m_summary.percentCumulative;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_VarianceCumulative(double* value) noexcept override
        try
        {
            *value = m_summary.varianceCumulative;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_Sigma4Cumulative(double* value) noexcept override
        try
        {
            *value = m_summary.sigma4Cumulative;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_MaxPercent(double* value) noexcept override
        try
        {
            *value = m_summary.maxPercent;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_SamplesAboveThreshold(unsigned __int32* value) noexcept override
        try
        {
            *value = m_summary.samplesAboveThreshold;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_TotalCpuTimeInMicroseconds(unsigned __int64* value) noexcept override
        try
        {
            *value = m_summary.totalCpuTimeInMicroseconds;
            return S_OK;
        }
        CATCH_RETURN()

    private:
        ProcessPerformanceSummary m_summary;
    };
}

namespace ABI::DevHome::QuietBackgroundProcesses
{
    class ProcessPerformanceTable :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IProcessPerformanceTable,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_QuietBackgroundProcesses_ProcessPerformanceTable, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize(unique_process_utilization_monitoring_thread context) noexcept
        {
            m_context = std::move(context);
            return S_OK;
        }

        STDMETHODIMP get_Rows(unsigned int* valueLength, ABI::DevHome::QuietBackgroundProcesses::IProcessRow*** value) noexcept override
        try
        {
            wil::unique_cotaskmem_array_ptr<ProcessPerformanceSummary> summaries;
            THROW_IF_FAILED(GetMonitoringProcessUtilization(m_context.get(), summaries.addressof(), summaries.size_address()));

            // Add rows
            auto list = make_unique_comptr_array<IProcessRow>(summaries.size());
            for (uint32_t i = 0; i < summaries.size(); i++)
            {
                auto& summary = summaries[i];
                wil::com_ptr<ProcessRow> row;
                THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<ProcessRow>(&row, summary));
                list[i] = std::move(row);
            }
            *valueLength = static_cast<unsigned int>(summaries.size());
            *value = (ABI::DevHome::QuietBackgroundProcesses::IProcessRow**)list.release();
            return S_OK;
        }
        CATCH_RETURN()

    private:
        unique_process_utilization_monitoring_thread m_context;
    };
}

namespace ABI::DevHome::QuietBackgroundProcesses
{
    class PerformanceRecorderEngine :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IPerformanceRecorderEngine,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_QuietBackgroundProcesses_PerformanceRecorderEngine, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize() noexcept
        {
            return S_OK;
        }

        // IPerformanceRecorderEngine
        STDMETHODIMP Start(ABI::Windows::Foundation::TimeSpan samplingPeriod) noexcept override
        try
        {
            // Convert TimeSpan from 100ns to milliseconds
            auto periodInMs = static_cast<uint32_t>(samplingPeriod.Duration / 10000);
            THROW_IF_FAILED(StartMonitoringProcessUtilization(periodInMs, &m_context));
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP Stop(ABI::DevHome::QuietBackgroundProcesses::IProcessPerformanceTable** result) noexcept override
        try
        {
            THROW_IF_FAILED(StopMonitoringProcessUtilization(m_context.get()));

            if (result)
            {
                wil::com_ptr<ProcessPerformanceTable> performanceTable;
                THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<ProcessPerformanceTable>(&performanceTable, std::move(m_context)));
                *result = performanceTable.detach();
            }

            return S_OK;
        }
        CATCH_RETURN()

    private:
        unique_process_utilization_monitoring_thread m_context;
    };

    ActivatableClass(PerformanceRecorderEngine);
}
