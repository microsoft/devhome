#pragma once
#include "ComputeSystemsResult.g.h"

using namespace winrt::Windows::Foundation::Collections;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemsResult : ComputeSystemsResultT<ComputeSystemsResult>
    {
        ComputeSystemsResult(IIterable<IComputeSystem> const& computeSystems);
        ComputeSystemsResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        IIterable<IComputeSystem> ComputeSystems();
        ProviderOperationResult Result();

    private:
        IIterable<IComputeSystem> m_computeSystems;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemsResult : ComputeSystemsResultT<ComputeSystemsResult, implementation::ComputeSystemsResult>
    {
    };
}
