#include "pch.h"
#include "ComputeSystemOperationResult.h"
#include "ComputeSystemOperationResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemOperationResult::ComputeSystemOperationResult()
        : m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemOperationResult::ComputeSystemOperationResult(winrt::hresult const& e, hstring const& diagnosticText)
        : m_result(ProviderOperationStatus::Failure, e, diagnosticText, diagnosticText)
    {
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult ComputeSystemOperationResult::Result()
    {
        return m_result;
    }
}