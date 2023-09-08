#pragma once
#include "AdaptiveCardSessionResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct AdaptiveCardSessionResult : AdaptiveCardSessionResultT<AdaptiveCardSessionResult>
    {
        AdaptiveCardSessionResult() = default;

        AdaptiveCardSessionResult(winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession const& adaptiveCardSession);
        AdaptiveCardSessionResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession AdaptiveCardSession();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        std::shared_ptr<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult> _Result;
        std::shared_ptr<winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession> _AdaptiveCardSession;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct AdaptiveCardSessionResult : AdaptiveCardSessionResultT<AdaptiveCardSessionResult, implementation::AdaptiveCardSessionResult>
    {
    };
}
