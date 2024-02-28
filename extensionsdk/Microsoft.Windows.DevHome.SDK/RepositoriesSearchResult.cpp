#include "pch.h"
#include "RepositoriesSearchResult.h"
#include "RepositoriesSearchResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories)
    {
        throw hresult_not_implemented();
    }
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories, hstring const& searchPath, array_view<hstring const> selectionOptions, hstring const& selectionOptionsName)
    {
        throw hresult_not_implemented();
    }
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> RepositoriesSearchResult::Repositories()
    {
        throw hresult_not_implemented();
    }
    hstring RepositoriesSearchResult::SearchPath()
    {
        throw hresult_not_implemented();
    }
    com_array<hstring> RepositoriesSearchResult::SelectionOptions()
    {
        throw hresult_not_implemented();
    }
    hstring RepositoriesSearchResult::SelectionOptionsName()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoriesSearchResult::Result()
    {
        throw hresult_not_implemented();
    }
}
