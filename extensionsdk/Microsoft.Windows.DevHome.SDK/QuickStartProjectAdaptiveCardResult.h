#pragma once
#include "QuickStartProjectAdaptiveCardResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct QuickStartProjectAdaptiveCardResult : QuickStartProjectAdaptiveCardResultT<QuickStartProjectAdaptiveCardResult>
    {
        QuickStartProjectAdaptiveCardResult(winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession2 const& adaptiveCardSession);
        QuickStartProjectAdaptiveCardResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession2 AdaptiveCardSession();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        IExtensionAdaptiveCardSession2 m_adaptiveCardSession;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct QuickStartProjectAdaptiveCardResult : QuickStartProjectAdaptiveCardResultT<QuickStartProjectAdaptiveCardResult, implementation::QuickStartProjectAdaptiveCardResult>
    {
    };
}
