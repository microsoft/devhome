#include "pch.h"
#include "RepositoriesSearchResult.h"
#include "RepositoriesSearchResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories) :
        _Repositories(std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>>(repositories)), 
        _SelectionsOptionsLabel(L""), 
        _SelectionOptionsName(L""), 
        _SelectionOptions(std::vector<hstring>()),
        _Result(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring("")))
    {
    }

    RepositoriesSearchResult::RepositoriesSearchResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories, hstring const& selectionsOptionsLabel, array_view<hstring const> selectionOptions, hstring const& selectionOptionsName) :
        _Repositories(std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>>(repositories)),
        _SelectionsOptionsLabel(selectionsOptionsLabel),
        _SelectionOptionsName(selectionOptionsName),
        _SelectionOptions(std::vector<hstring>{ selectionOptions.begin(), selectionOptions.end() }),
        _Result(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring("")))
    {
    }

    RepositoriesSearchResult::RepositoriesSearchResult(winrt::hresult const& e, hstring const& diagnosticText) :
        _Repositories(std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>>(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>())),
        _SelectionsOptionsLabel(L""),
        _SelectionOptionsName(L""),
        _SelectionOptions(std::vector<hstring>()),
        _Result(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), diagnosticText, diagnosticText))
    {
    }

    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> RepositoriesSearchResult::Repositories()
    {
        return *_Repositories.get();
    }

    hstring RepositoriesSearchResult::SelectionsOptionsLabel()
    {
        return _SelectionsOptionsLabel;
    }

    com_array<hstring> RepositoriesSearchResult::SelectionOptions()
    {
        return winrt::com_array<hstring>{ _SelectionOptions };
    }

    hstring RepositoriesSearchResult::SelectionOptionsName()
    {
        return _SelectionOptionsName;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoriesSearchResult::Result()
    {
        return _Result;
    }
}
