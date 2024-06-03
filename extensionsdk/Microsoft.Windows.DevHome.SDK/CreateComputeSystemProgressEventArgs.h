#pragma once
#include "CreateComputeSystemProgressEventArgs.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct CreateComputeSystemProgressEventArgs : CreateComputeSystemProgressEventArgsT<CreateComputeSystemProgressEventArgs>
    {
        CreateComputeSystemProgressEventArgs(hstring const& operationStatus, uint32_t percentageCompleted);

        hstring Status();
        uint32_t PercentageCompleted();

    private:
        hstring m_status;
        uint32_t m_percentageCompleted;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct CreateComputeSystemProgressEventArgs : CreateComputeSystemProgressEventArgsT<CreateComputeSystemProgressEventArgs, implementation::CreateComputeSystemProgressEventArgs>
    {
    };
}
