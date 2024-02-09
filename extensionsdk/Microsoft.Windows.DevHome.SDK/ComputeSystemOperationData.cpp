#include "pch.h"
#include "ComputeSystemOperationData.h"
#include "ComputeSystemOperationData.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemOperationData::ComputeSystemOperationData(hstring const& operationStatus, uint32_t operationProgress)
        : m_status(operationStatus), m_progress(operationProgress)
    {
    }

    hstring ComputeSystemOperationData::Status()
    {
        return m_status;
    }
    uint32_t ComputeSystemOperationData::Progress()
    {
        return m_progress;
    }
}
