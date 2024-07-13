#include "pch.h"
#include "DeveloperIdResult.h"
#include "DeveloperIdResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    DeveloperIdResult::DeveloperIdResult(winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId const& developerId)
    {
        throw hresult_not_implemented();
    }
    DeveloperIdResult::DeveloperIdResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId DeveloperIdResult::DeveloperId()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult DeveloperIdResult::Result()
    {
        throw hresult_not_implemented();
    }
}
