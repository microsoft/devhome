#include "pch.h"
#include "ComputeSystemStateResult.h"
#include "ComputeSystemStateResult.g.cpp"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemStateResult::ComputeSystemStateResult(ComputeSystemState const& computeSystemState) :
        m_computeSystemState(computeSystemState), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemStateResult::ComputeSystemStateResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_computeSystemState(ComputeSystemState::Unknown), m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }

    ComputeSystemState ComputeSystemStateResult::State()
    {
        return m_computeSystemState;
    }

    ProviderOperationResult ComputeSystemStateResult::Result()
    {
        return m_result;
    }
}
