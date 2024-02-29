#include "pch.h"
#include "RepositoriesSearchResult.h"
#include "RepositoriesSearchResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories)
    {
        _Repositories = std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>>(repositories);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }

    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories, hstring const& searchPath, array_view<hstring const> selectionOptions, hstring const& selectionOptionsName)
    {
        _Repositories = std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>>(repositories);
        _SearchPath = std::make_shared<hstring>(searchPath);
        _SelectionOptions = std::make_shared<array_view<hstring const>>();
        _SelectionOptions.reset(&selectionOptions);
        _SelectionOptionsName = std::make_shared<hstring>(selectionOptionsName);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }

    RepositoriesSearchResult::RepositoriesSearchResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
    }

    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> RepositoriesSearchResult::Repositories()
    {
        return *_Repositories.get();
    }

    hstring RepositoriesSearchResult::SearchPath()
    {
        return *_SearchPath.get();
    }

    com_array<hstring> RepositoriesSearchResult::SelectionOptions()
    {
        return winrt::com_array<hstring>{ _SelectionOptions.get()->begin(), _SelectionOptions.get()->end() };
    }

    hstring RepositoriesSearchResult::SelectionOptionsName()
    {
        return *_SelectionOptionsName.get();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoriesSearchResult::Result()
    {
        return *_Result.get();
    }
}
