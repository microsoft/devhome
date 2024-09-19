#pragma once
#include "NavigationPagesResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct NavigationPagesResult : NavigationPagesResultT<NavigationPagesResult>
    {
        NavigationPagesResult() = default;

        NavigationPagesResult(array_view<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage const> navigationPages);
        NavigationPagesResult(winrt::hresult const& e, hstring const& diagnosticText);
        com_array<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage> NavigationPages();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct NavigationPagesResult : NavigationPagesResultT<NavigationPagesResult, implementation::NavigationPagesResult>
    {
    };
}