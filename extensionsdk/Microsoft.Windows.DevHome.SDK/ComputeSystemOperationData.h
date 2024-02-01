#pragma once
#include "ComputeSystemOperationData.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemOperationData : ComputeSystemOperationDataT<ComputeSystemOperationData>
    {
        ComputeSystemOperationData(hstring const& OperationStatus, uint32_t OperationProgress);
        hstring Status();
        uint32_t Progress();

    private:
        hstring m_status;
        uint32_t m_progress;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemOperationData : ComputeSystemOperationDataT<ComputeSystemOperationData, implementation::ComputeSystemOperationData>
    {
    };
}
