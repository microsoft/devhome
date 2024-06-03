#include "pch.h"
#include "ApplyConfigurationResult.h"
#include "ApplyConfigurationResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ApplyConfigurationResult::ApplyConfigurationResult(Projection::OpenConfigurationSetResult const& openConfigurationSetResult, Projection::ApplyConfigurationSetResult const& applyConfigurationSetResult) :
        m_openConfigurationSetResult(openConfigurationSetResult), m_applyConfigurationSetResult(applyConfigurationSetResult), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ApplyConfigurationResult::ApplyConfigurationResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }

    ProviderOperationResult ApplyConfigurationResult::Result()
    {
       return m_result;
    }

    OpenConfigurationSetResult ApplyConfigurationResult::OpenConfigurationSetResult()
    {
        return m_openConfigurationSetResult;
    }

    ApplyConfigurationSetResult ApplyConfigurationResult::ApplyConfigurationSetResult()
    {
        return m_applyConfigurationSetResult;
    }
}
