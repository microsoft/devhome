#pragma once
#include "CreateComputeSystemResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct CreateComputeSystemResult : CreateComputeSystemResultT<CreateComputeSystemResult>
    {
        CreateComputeSystemResult() = default;

        CreateComputeSystemResult(IComputeSystem const& computeSystem);
        CreateComputeSystemResult(winrt::hresult const& e, hstring const& diagnosticText);
        IComputeSystem ComputeSystem();
        ProviderOperationResult Result();

    private:
        IComputeSystem m_computeSystem;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct CreateComputeSystemResult : CreateComputeSystemResultT<CreateComputeSystemResult, implementation::CreateComputeSystemResult>
    {
    };
}
