#include "pch.h"
#include "QuickStartProjectAdaptiveCardResult.h"
#include "QuickStartProjectAdaptiveCardResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    QuickStartProjectAdaptiveCardResult::QuickStartProjectAdaptiveCardResult(winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession2 const& adaptiveCardSession) :
        m_adaptiveCardSession(adaptiveCardSession),
        m_result(ProviderOperationStatus::Success, S_OK, hstring{}, hstring{})
    {
    }

    QuickStartProjectAdaptiveCardResult::QuickStartProjectAdaptiveCardResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_adaptiveCardSession(nullptr),
        m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }

    winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession2 QuickStartProjectAdaptiveCardResult::AdaptiveCardSession()
    {
        return m_adaptiveCardSession;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult QuickStartProjectAdaptiveCardResult::Result()
    {
        return m_result;
    }
}
