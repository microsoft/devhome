// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <chrono>
#include <memory>
#include <mutex>
#include <span>

#include <wrl/client.h>
#include <wrl/implements.h>
#include <wrl/module.h>

#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include <Windows.Foundation.h>
#include <Windows.Foundation.Collections.h>

#include "Common.h"
#include "TimedQuietSession.h"
#include "DevHome.QuietBackgroundProcesses.h"
#include "PerformanceRecorderEngine.h"
#include "Helpers.h"


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

        STDMETHODIMP get_ServiceName(HSTRING* value) noexcept override
        try
        {
            Microsoft::WRL::Wrappers::HString str;
            str.Set(m_summary.serviceName);
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
            std::span<ProcessPerformanceSummary> span;
            wil::unique_cotaskmem_array_ptr<ProcessPerformanceSummary> summariesCoarray;
            std::vector<ProcessPerformanceSummary> summariesVector;

            if (m_context)
            {
                // We have a live context, read performance data from it
                THROW_IF_FAILED(GetMonitoringProcessUtilization(m_context.get(), nullptr, summariesCoarray.addressof(), summariesCoarray.size_address()));

                // Make span from cotaskmem_array
                span = std::span<ProcessPerformanceSummary>{ summariesCoarray.get(), summariesCoarray.size() };
            }
            else
            {
                // We don't have a live context. Let's try to read performance data from disk.
                auto performanceDataFile = GetTemporaryPerformanceDataPath();
                THROW_HR_IF(E_FAIL, !std::filesystem::exists(performanceDataFile));

                // Make span from vector
                summariesVector = ReadPerformanceDataFromDisk(performanceDataFile.c_str());
                span = std::span<ProcessPerformanceSummary>{ summariesVector };
            }

            // Create IProcessRow entries
            auto list = make_unique_comptr_array<IProcessRow>(span.size());
            for (uint32_t i = 0; i < span.size(); i++)
            {
                auto& summary = span[i];
                wil::com_ptr<ProcessRow> row;
                THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<ProcessRow>(&row, summary));
                list[i] = std::move(row);
            }
            *valueLength = static_cast<unsigned int>(span.size());
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

            {
                // Get the performance data from the monitoring engine
                std::chrono::milliseconds samplingPeriod;
                wil::unique_cotaskmem_array_ptr<ProcessPerformanceSummary> summaries;
                THROW_IF_FAILED(GetMonitoringProcessUtilization(m_context.get(), &samplingPeriod, summaries.addressof(), summaries.size_address()));
                std::span<ProcessPerformanceSummary> data(summaries.get(), summaries.size());

                // Write the performance .csv data to disk (if Dev Home is closed, enables user to see the Analytic Summary later)
                try
                {
                    auto performanceDataFile = GetTemporaryPerformanceDataPath();
                    WritePerformanceDataToDisk(performanceDataFile.c_str(), data);
                }
                CATCH_LOG();

                // Upload the performance data telemetry
                try
                {
                    UploadPerformanceDataTelemetry(samplingPeriod, data);
                }
                CATCH_LOG();
            }

            if (result)
            {
                wil::com_ptr<ProcessPerformanceTable> performanceTable;
                THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<ProcessPerformanceTable>(&performanceTable, std::move(m_context)));
                *result = performanceTable.detach();
            }

            // Destroy the performance engine instance
            m_context.reset();

            return S_OK;
        }
        CATCH_RETURN()

    private:
        unique_process_utilization_monitoring_thread m_context;
    };

    class PerformanceRecorderEngineStatics WrlFinal :
        public Microsoft::WRL::AgileActivationFactory<
            Microsoft::WRL::Implements<IPerformanceRecorderEngineStatics>>
    {
        InspectableClassStatic(RuntimeClass_DevHome_QuietBackgroundProcesses_PerformanceRecorderEngine, BaseTrust);

    public:
        STDMETHODIMP ActivateInstance(_COM_Outptr_ IInspectable**) noexcept
        {
            // Disallow activation - must use GetSingleton()
            return E_NOTIMPL;
        }

        // IPerformanceRecorderEngineStatics
        STDMETHODIMP TryGetLastPerformanceRecording(_COM_Outptr_ ABI::DevHome::QuietBackgroundProcesses::IProcessPerformanceTable** result) noexcept override
        try
        {
            // Reconstruct a perform table from disk (passing nullptr for context)
            wil::com_ptr<ProcessPerformanceTable> performanceTable;
            THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<ProcessPerformanceTable>(&performanceTable, nullptr));
            *result = performanceTable.detach();
            
            return S_OK;
        }
        CATCH_RETURN()
    };

    ActivatableClassWithFactory(PerformanceRecorderEngine, PerformanceRecorderEngineStatics);
}

wil::com_ptr<ABI::DevHome::QuietBackgroundProcesses::IPerformanceRecorderEngine> MakePerformanceRecorderEngine()
{
    using namespace ABI::DevHome::QuietBackgroundProcesses;
    wil::com_ptr<PerformanceRecorderEngine> result;
    THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<PerformanceRecorderEngine>(&result));
    return result;
}
