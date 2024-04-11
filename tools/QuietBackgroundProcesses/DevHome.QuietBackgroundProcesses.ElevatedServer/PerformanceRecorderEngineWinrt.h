// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <wrl/client.h>
#include <wrl/implements.h>

#include "DevHome.QuietBackgroundProcesses.h"
#include "PerformanceRecorderEngine.h"

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
        STDMETHODIMP RuntimeClassInitialize() noexcept;

        // IPerformanceRecorderEngine
        STDMETHODIMP Start(ABI::Windows::Foundation::TimeSpan samplingPeriod) noexcept override;

        STDMETHODIMP Stop(ABI::DevHome::QuietBackgroundProcesses::IProcessPerformanceTable** result) noexcept override;
    private:
        unique_process_utilization_monitoring_thread m_context;
    };
}
