#include "pch.h"
#include "ComputeSystemStateResult.h"
#include "ComputeSystemStateResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemStateResult::ComputeSystemStateResult(ComputeSystemState const& computeSystemState) 
        : m_computeSystemStates(computeSystemState), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemStateResult::ComputeSystemStateResult(winrt::hresult const& e, hstring const& diagnosticText)
        : m_computeSystemStates(ComputeSystemState::Unknown), m_result(ProviderOperationStatus::Failure, e, diagnosticText, diagnosticText)
    {
    }

    ComputeSystemState ComputeSystemStateResult::GetComputeSystemState()
    {
        return m_computeSystemStates;
    }

    ProviderOperationResult ComputeSystemStateResult::Result()
    {
        return m_result;
    }
}
