#pragma once
#include "NavigationPagesResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct NavigationPagesResult : NavigationPagesResultT<NavigationPagesResult>
    {
        NavigationPagesResult() = default;

        NavigationPagesResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage> const& navigationPages);
        NavigationPagesResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage> NavigationPages();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        std::shared_ptr<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult> _Result;
        std::shared_ptr<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::INavigationPage>> _NavigationPages;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct NavigationPagesResult : NavigationPagesResultT<NavigationPagesResult, implementation::NavigationPagesResult>
    {
    };
}
