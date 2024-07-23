#pragma once
#include "ComputeSystemOperationResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemOperationResult : ComputeSystemOperationResultT<ComputeSystemOperationResult>
    {
        ComputeSystemOperationResult() = default;

        ComputeSystemOperationResult(hstring const& jsonResponseData);
        ComputeSystemOperationResult(winrt::hresult const& e, hstring const& diagnosticText, hstring const& jsonResponseData);
        hstring JsonResponseData();
        ProviderOperationResult Result();

    private:
        hstring m_jsonResponseData;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemOperationResult : ComputeSystemOperationResultT<ComputeSystemOperationResult, implementation::ComputeSystemOperationResult>
    {
    };
}
