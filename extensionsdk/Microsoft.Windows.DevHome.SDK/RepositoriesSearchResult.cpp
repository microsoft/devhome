#include "pch.h"
#include "RepositoriesSearchResult.h"
#include "RepositoriesSearchResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories)
    {
        _Repositories = repositories;
        _SelectionOptionsLabel = hstring(L"");
        _SelectionOptions = std::vector<hstring>();
        _SelectionOptionsName = hstring(L"");
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }

    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories, hstring const& searchPath, array_view<hstring const> selectionOptions, hstring const& selectionOptionsName)
    {
        _Repositories = repositories;
        _SelectionOptionsLabel = searchPath;
        _SelectionOptions = std::vector<hstring>{ selectionOptions.begin(), selectionOptions.end() };
        _SelectionOptionsName = selectionOptionsName;
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Repositories = winrt::Windows::Foundation::Collections::IIterable<IRepository>();
        _SelectionOptionsLabel = hstring(L"");
        _SelectionOptions = std::vector<hstring>();
        _SelectionOptionsName = hstring(L"");
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
    }

    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> RepositoriesSearchResult::Repositories()
    {
        return _Repositories;
    }

    hstring RepositoriesSearchResult::SelectionsOptionLabel()
    {
        return _SelectionOptionsLabel;
    }

    com_array<hstring> RepositoriesSearchResult::SelectionOptions()
    {
        return com_array<hstring>{ _SelectionOptions.begin(), _SelectionOptions.end() };
    }

    hstring RepositoriesSearchResult::SelectionOptionsName()
    {
        return _SelectionOptionsName;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoriesSearchResult::Result()
    {
        return *_Result.get();
    }
}
