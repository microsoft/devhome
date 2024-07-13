#include "pch.h"
#include "AdaptiveCardSessionResult.h"
#include "AdaptiveCardSessionResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    AdaptiveCardSessionResult::AdaptiveCardSessionResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> const& developerIds)
    {
        throw hresult_not_implemented();
    }
    AdaptiveCardSessionResult::AdaptiveCardSessionResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession AdaptiveCardSessionResult::AdaptiveCardSession()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult AdaptiveCardSessionResult::Result()
    {
        throw hresult_not_implemented();
    }
}
