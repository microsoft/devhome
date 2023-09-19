#include "pch.h"
#include "AdaptiveCardSessionResult.h"
#include "AdaptiveCardSessionResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    AdaptiveCardSessionResult::AdaptiveCardSessionResult(winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession const& adaptiveCardSession)
    {
        _AdaptiveCardSession = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession>(adaptiveCardSession);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }
    AdaptiveCardSessionResult::AdaptiveCardSessionResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
    }
    winrt::Microsoft::Windows::DevHome::SDK::IExtensionAdaptiveCardSession AdaptiveCardSessionResult::AdaptiveCardSession()
    {
        return *_AdaptiveCardSession.get();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult AdaptiveCardSessionResult::Result()
    {
        return *_Result.get();
    }
}
