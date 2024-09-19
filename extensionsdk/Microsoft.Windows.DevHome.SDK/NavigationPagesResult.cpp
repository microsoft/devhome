#include "pch.h"
#include "NavigationPagesResult.h"
#include "NavigationPagesResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    NavigationPagesResult::NavigationPagesResult(array_view<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage const> navigationPages)
    {
        throw hresult_not_implemented();
    }
    NavigationPagesResult::NavigationPagesResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    com_array<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage> NavigationPagesResult::NavigationPages()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult NavigationPagesResult::Result()
    {
        throw hresult_not_implemented();
    }
}