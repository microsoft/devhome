#pragma once
#include "RepositoriesSearchResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct RepositoriesSearchResult : RepositoriesSearchResultT<RepositoriesSearchResult>
    {
        RepositoriesSearchResult() = default;

        RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories);
        RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories, hstring const& searchPath, array_view<hstring const> selectionOptions, hstring const& selectionOptionsName);
        RepositoriesSearchResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> Repositories();
        hstring SearchPath();
        com_array<hstring> SelectionOptions();
        hstring SelectionOptionsName();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct RepositoriesSearchResult : RepositoriesSearchResultT<RepositoriesSearchResult, implementation::RepositoriesSearchResult>
    {
    };
}
