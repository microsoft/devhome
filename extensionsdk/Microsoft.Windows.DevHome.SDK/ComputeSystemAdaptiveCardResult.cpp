#include "pch.h"
#include "ComputeSystemAdaptiveCardResult.h"
#include "ComputeSystemAdaptiveCardResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemAdaptiveCardResult::ComputeSystemAdaptiveCardResult(IExtensionAdaptiveCardSession2 const& cardSession)
        : m_computeSystemCardSession(cardSession), m_result(ProviderOperationStatus::Success, S_OK, hstring{}, hstring{})
    {
    }

    ComputeSystemAdaptiveCardResult::ComputeSystemAdaptiveCardResult(winrt::hresult const& e, hstring const& diagnosticText) 
        : m_computeSystemCardSession(nullptr), m_result(ProviderOperationStatus::Failure, e, hstring{}, diagnosticText)
    {
    }
    winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession2 ComputeSystemAdaptiveCardResult::ComputeSystemCardSession()
    {
        return m_computeSystemCardSession;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult ComputeSystemAdaptiveCardResult::Result()
    {
        return m_result;
    }
}
