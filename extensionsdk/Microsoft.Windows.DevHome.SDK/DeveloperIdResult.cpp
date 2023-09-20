#include "pch.h"
#include "DeveloperIdResult.h"
#include "DeveloperIdResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    DeveloperIdResult::DeveloperIdResult(winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId const& developerId)
    {
        _DeveloperId = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId>(developerId);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }
    DeveloperIdResult::DeveloperIdResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
    }
    winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId DeveloperIdResult::DeveloperId()
    {
        return *_DeveloperId.get();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult DeveloperIdResult::Result()
    {
        return *_Result.get();
    }
}
