#include "pch.h"
#include "CreateComputeSystemProgressEventArgs.h"
#include "CreateComputeSystemProgressEventArgs.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    CreateComputeSystemProgressEventArgs::CreateComputeSystemProgressEventArgs(hstring const& operationStatus, uint32_t percentageCompleted) :
        m_status(operationStatus), m_percentageCompleted(percentageCompleted)
    {
    }

    hstring CreateComputeSystemProgressEventArgs::Status()
    {
       return m_status;
    }

    uint32_t CreateComputeSystemProgressEventArgs::PercentageCompleted()
    {
        return m_percentageCompleted;
    }
}
