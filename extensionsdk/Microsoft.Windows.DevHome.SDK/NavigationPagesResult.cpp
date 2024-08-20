#include "pch.h"
#include "NavigationPagesResult.h"
#include "NavigationPagesResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    NavigationPagesResult::NavigationPagesResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage> const& navigationPages)
    {
        _NavigationPages = std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage>>(navigationPages);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }
    NavigationPagesResult::NavigationPagesResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), diagnosticText, diagnosticText);
    }
    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage> NavigationPagesResult::NavigationPages()
    {
        return *_NavigationPages.get();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult NavigationPagesResult::Result()
    {
        return *_Result.get();
    }
}
