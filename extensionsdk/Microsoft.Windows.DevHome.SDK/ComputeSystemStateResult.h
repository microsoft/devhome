#pragma once
#include "ComputeSystemStateResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemStateResult : ComputeSystemStateResultT<ComputeSystemStateResult>
    {
        ComputeSystemStateResult() = default;

        ComputeSystemStateResult(ComputeSystemState const& computeSystemState);
        ComputeSystemStateResult(winrt::hresult const& e, hstring const& diagnosticText);
        ComputeSystemState GetComputeSystemState();
        ProviderOperationResult Result();

    private:
        ComputeSystemState m_computeSystemStates;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemStateResult : ComputeSystemStateResultT<ComputeSystemStateResult, implementation::ComputeSystemStateResult>
    {
    };
}
