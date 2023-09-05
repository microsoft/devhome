#pragma once
#include "AdaptiveCardSessionResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct AdaptiveCardSessionResult : AdaptiveCardSessionResultT<AdaptiveCardSessionResult>
    {
        AdaptiveCardSessionResult() = default;

        AdaptiveCardSessionResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> const& developerIds);
        AdaptiveCardSessionResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession AdaptiveCardSession();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct AdaptiveCardSessionResult : AdaptiveCardSessionResultT<AdaptiveCardSessionResult, implementation::AdaptiveCardSessionResult>
    {
    };
}
