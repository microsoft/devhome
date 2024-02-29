#include "pch.h"
#include "ComputeSystemOperationResult.h"
#include "ComputeSystemOperationResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemOperationResult::ComputeSystemOperationResult() :
        m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemOperationResult::ComputeSystemOperationResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }

    ProviderOperationResult ComputeSystemOperationResult::Result()
    {
        return m_result;
    }
}
