#include "pch.h"
#include "ComputeSystemsResult.h"
#include "ComputeSystemsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemsResult::ComputeSystemsResult(IIterable<IComputeSystem> const& computeSystems) 
        : m_computeSystems(computeSystems), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemsResult::ComputeSystemsResult(winrt::hresult const& e, hstring const& diagnosticText) 
        : m_computeSystems(nullptr), m_result(ProviderOperationStatus::Failure, e, diagnosticText, diagnosticText)
    {
    }

    IIterable<IComputeSystem> ComputeSystemsResult::ComputeSystems()
    {
        return m_computeSystems;
    }

    ProviderOperationResult ComputeSystemsResult::Result()
    {
        return m_result;
    }
}
