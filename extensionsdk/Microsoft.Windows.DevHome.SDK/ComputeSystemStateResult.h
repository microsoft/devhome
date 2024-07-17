#pragma once
#include "ComputeSystemStateResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemStateResult : ComputeSystemStateResultT<ComputeSystemStateResult>
    {
        ComputeSystemStateResult(ComputeSystemState const& computeSystemState);
        ComputeSystemStateResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        ComputeSystemState State();
        ProviderOperationResult Result();

    private:
        ComputeSystemState m_computeSystemState;
        ProviderOperationResult m_result;

    };
}

namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemStateResult : ComputeSystemStateResultT<ComputeSystemStateResult, implementation::ComputeSystemStateResult>
    {
    };
}
