#pragma once
#include "ComputeSystemOperationResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemOperationResult : ComputeSystemOperationResultT<ComputeSystemOperationResult>
    {
        ComputeSystemOperationResult();

        ComputeSystemOperationResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        ProviderOperationResult Result();

    private:
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemOperationResult : ComputeSystemOperationResultT<ComputeSystemOperationResult, implementation::ComputeSystemOperationResult>
    {
    };
}
